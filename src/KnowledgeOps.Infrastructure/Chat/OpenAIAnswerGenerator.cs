using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Domain.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.Infrastructure.Chat;

/// <summary>
/// Optional OpenAI answer generator for premium/manual demo mode.
/// Requires Ai:OpenAI:ApiKey configured via user-secrets or environment variable.
/// Never logs, stores, or exposes the API key.
/// Uses only authorized retrieved context; does not create citations.
/// </summary>
internal sealed class OpenAIAnswerGenerator : IAiAnswerGenerator
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private readonly OpenAIAnswerGeneratorSettings _settings;
    private readonly string _apiKey;
    private readonly ILogger<OpenAIAnswerGenerator> _logger;

    public string ProviderName => "OpenAI";
    public string DefaultModelName => _settings.Model;

    // Production constructor — used by DI registration.
    public OpenAIAnswerGenerator(
        IOptions<OpenAIAnswerGeneratorSettings> settings,
        IConfiguration configuration,
        ILogger<OpenAIAnswerGenerator> logger)
        : this(new HttpClient(), settings, configuration, logger) { }

    // Test constructor — allows injecting a mock HttpClient.
    internal OpenAIAnswerGenerator(
        HttpClient httpClient,
        IOptions<OpenAIAnswerGeneratorSettings> settings,
        IConfiguration configuration,
        ILogger<OpenAIAnswerGenerator> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _apiKey = configuration["Ai:OpenAI:ApiKey"] ?? string.Empty;
        _logger = logger;
    }

    public async Task<AnswerGenerationResult> GenerateAsync(
        AnswerGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var model = request.ModelName ?? _settings.Model;

        var systemInstruction = !string.IsNullOrWhiteSpace(request.SystemInstruction)
            ? request.SystemInstruction
            : "You are a knowledge assistant. Answer using only the provided approved context.";

        var formattedContext = !string.IsNullOrWhiteSpace(request.FormattedContext)
            ? request.FormattedContext
            : string.Join("\n\n", request.AuthorizedChunks.Select(c => c.ChunkText));

        var userContent = $"Context:\n{formattedContext}\n\nQuestion: {request.UserQuestion}";

        var payload = new ChatCompletionRequest(
            model,
            [
                new ChatMessage("system", systemInstruction),
                new ChatMessage("user", userContent)
            ],
            _settings.MaxTokens,
            _settings.Temperature);

        string jsonPayload;
        try
        {
            jsonPayload = JsonSerializer.Serialize(payload, SerializerOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                "OpenAI request serialization failed. ProviderName={ProviderName}",
                ProviderName);
            return ProviderFailed("ProviderRequestSerializationFailed", model);
        }

        HttpResponseMessage httpResponse;
        try
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.ApiEndpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            httpRequest.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            httpResponse = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseContentRead,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning(
                "OpenAI request timed out. ProviderName={ProviderName}",
                ProviderName);
            return ProviderFailed("ProviderTimeout", model);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "OpenAI HTTP request failed. ProviderName={ProviderName}",
                ProviderName);
            return ProviderFailed("ProviderUnavailable", model);
        }

        if (!httpResponse.IsSuccessStatusCode)
        {
            string? openAiErrorType = null;
            string? openAiErrorCode = null;
            try
            {
                var errorBodyText = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                using var errorDoc = JsonDocument.Parse(errorBodyText);
                if (errorDoc.RootElement.TryGetProperty("error", out var errorEl))
                {
                    if (errorEl.TryGetProperty("type", out var typeEl))
                        openAiErrorType = typeEl.GetString();
                    if (errorEl.TryGetProperty("code", out var codeEl))
                        openAiErrorCode = codeEl.GetString();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(
                    ex,
                    "Could not parse OpenAI error body. ProviderName={ProviderName}",
                    ProviderName);
            }

            var failureCode = MapOpenAiFailureCode((int)httpResponse.StatusCode, openAiErrorType, openAiErrorCode);

            _logger.LogWarning(
                "OpenAI returned non-success status. ProviderName={ProviderName} StatusCode={StatusCode} " +
                "OpenAiErrorType={OpenAiErrorType} OpenAiErrorCode={OpenAiErrorCode} FailureCode={FailureCode}",
                ProviderName,
                (int)httpResponse.StatusCode,
                openAiErrorType ?? "(none)",
                openAiErrorCode ?? "(none)",
                failureCode);

            return ProviderFailed(failureCode, model);
        }

        string responseBody;
        try
        {
            responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "OpenAI response read failed. ProviderName={ProviderName}",
                ProviderName);
            return ProviderFailed("ProviderResponseReadFailed", model);
        }

        string? answerText;
        int? inputTokens = null;
        int? outputTokens = null;

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            answerText = null;
            if (root.TryGetProperty("choices", out var choices)
                && choices.GetArrayLength() > 0
                && choices[0].TryGetProperty("message", out var message)
                && message.TryGetProperty("content", out var content))
            {
                answerText = content.GetString();
            }

            if (root.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var promptTokens))
                    inputTokens = promptTokens.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var completionTokens))
                    outputTokens = completionTokens.GetInt32();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "OpenAI response JSON parse failed. ProviderName={ProviderName}",
                ProviderName);
            return ProviderFailed("ProviderMalformedResponse", model);
        }

        if (string.IsNullOrWhiteSpace(answerText))
        {
            _logger.LogWarning(
                "OpenAI returned empty answer text. ProviderName={ProviderName}",
                ProviderName);
            return ProviderFailed("ProviderMalformedResponse", model);
        }

        return new AnswerGenerationResult(
            State: AnswerState.Grounded,
            AnswerText: answerText,
            InputTokens: inputTokens,
            OutputTokens: outputTokens,
            ModelUsed: model,
            ProviderName: ProviderName,
            SafeFailureCode: null);
    }

    // Maps an OpenAI HTTP status and optional parsed error type/code to a safe internal
    // provider failure code. Quota errors are mapped before rate-limit errors so that
    // insufficient_quota (429 with quota body) is never misclassified as ProviderRateLimited.
    private static string MapOpenAiFailureCode(int statusCode, string? errorType, string? errorCode)
    {
        // Quota exhaustion — must take precedence over generic rate-limit
        if (IsMatch(errorType, "insufficient_quota") || IsMatch(errorCode, "insufficient_quota"))
            return "ProviderQuotaExceeded";

        // Rate limiting (tokens/requests per minute or per day)
        if (IsMatch(errorType, "rate_limit_exceeded") || IsMatch(errorCode, "rate_limit_exceeded")
            || IsMatch(errorCode, "tokens") || IsMatch(errorCode, "requests"))
            return "ProviderRateLimited";

        // Authentication / key errors
        if (IsMatch(errorCode, "invalid_api_key") || IsMatch(errorCode, "incorrect_api_key"))
            return "ProviderUnauthorized";

        // Model not available
        if (IsMatch(errorCode, "model_not_found") || IsMatch(errorCode, "model_decommissioned"))
            return "ProviderModelUnavailable";

        // Invalid request (bad parameters, context too long, etc.)
        if (IsMatch(errorType, "invalid_request_error")
            || IsMatch(errorCode, "invalid_request_error")
            || IsMatch(errorCode, "context_length_exceeded")
            || IsMatch(errorCode, "max_tokens_exceeded"))
            return "ProviderInvalidRequest";

        // Server-side errors from OpenAI
        if (IsMatch(errorType, "server_error") || IsMatch(errorType, "service_unavailable"))
            return "ProviderUnavailable";

        // HTTP status fallback when error body was absent or unparseable
        return statusCode switch
        {
            401 or 403 => "ProviderUnauthorized",
            400 or 422 => "ProviderInvalidRequest",
            404 => "ProviderModelUnavailable",
            408 => "ProviderTimeout",
            409 => "ProviderFailed",
            429 => "ProviderRateLimited",
            >= 500 => "ProviderUnavailable",
            _ => "ProviderFailed"
        };
    }

    private static bool IsMatch(string? value, string target) =>
        string.Equals(value, target, StringComparison.OrdinalIgnoreCase);

    private AnswerGenerationResult ProviderFailed(string code, string? model = null) =>
        new(AnswerState.ProviderFailed, null, null, null, model, ProviderName, code);

    private sealed record ChatCompletionRequest(
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        int MaxTokens,
        double Temperature);

    private sealed record ChatMessage(string Role, string Content);
}

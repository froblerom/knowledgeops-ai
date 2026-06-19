using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Domain.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.Infrastructure.Chat;

/// <summary>
/// Optional local answer generator for use with any OpenAI-compatible runtime
/// (Ollama, LM Studio, vLLM, llama.cpp, etc.).
/// Requires Ai:LocalOpenAICompatible:BaseUrl configured in appsettings or environment.
/// No API key is required for Ollama; set Ai:LocalOpenAICompatible:ApiKey if needed.
/// Never logs, stores, or exposes the API key, prompt, or retrieved context.
/// </summary>
internal sealed class LocalOpenAICompatibleAnswerGenerator : IAiAnswerGenerator
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    // Matches <think>…</think> blocks including newlines (non-greedy).
    // Applies when providers (e.g. bare llama.cpp, some vLLM configs) embed reasoning as
    // XML tags directly inside the content field. Ollama's /v1/chat/completions endpoint
    // routes reasoning to a separate "reasoning" field instead, so this regex is a no-op
    // for Ollama — the content field already contains only the visible answer text.
    private static readonly Regex ThinkTagRegex =
        new(@"<think>[\s\S]*?</think>", RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly LocalOpenAICompatibleAnswerGeneratorSettings _settings;
    private readonly ILogger<LocalOpenAICompatibleAnswerGenerator> _logger;

    public string ProviderName => _settings.ProviderName;
    public string DefaultModelName => _settings.Model;

    // Production constructor — used by DI.
    public LocalOpenAICompatibleAnswerGenerator(
        IOptions<LocalOpenAICompatibleAnswerGeneratorSettings> settings,
        ILogger<LocalOpenAICompatibleAnswerGenerator> logger)
        : this(new HttpClient(), settings, logger) { }

    // Test constructor — allows injecting a fake HttpClient.
    internal LocalOpenAICompatibleAnswerGenerator(
        HttpClient httpClient,
        IOptions<LocalOpenAICompatibleAnswerGeneratorSettings> settings,
        ILogger<LocalOpenAICompatibleAnswerGenerator> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(settings.Value.TimeoutSeconds);
        _settings = settings.Value;
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
                "Local provider request serialization failed. ProviderName={ProviderName}",
                ProviderName);
            return ProviderFailed("ProviderRequestSerializationFailed", model);
        }

        var endpoint = $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions";

        HttpResponseMessage httpResponse;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);

            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
                httpRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

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
                "Local provider request timed out. ProviderName={ProviderName} Model={Model}",
                ProviderName, model);
            return ProviderFailed("ProviderTimeout", model);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Local provider HTTP request failed. ProviderName={ProviderName} Model={Model}",
                ProviderName, model);
            return ProviderFailed("ProviderUnavailable", model);
        }

        if (!httpResponse.IsSuccessStatusCode)
        {
            // Try to parse error body for precise failure code.
            // Local runtimes may return { "error": "string" } (not { "error": { "type", "code" } }).
            // The parse is best-effort; HTTP status fallback is used when error body is absent or opaque.
            string? openAiErrorType = null;
            string? openAiErrorCode = null;
            try
            {
                var errorBodyText = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                using var errorDoc = JsonDocument.Parse(errorBodyText);
                if (errorDoc.RootElement.TryGetProperty("error", out var errorEl)
                    && errorEl.ValueKind == JsonValueKind.Object)
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
                    "Could not parse local provider error body. ProviderName={ProviderName}",
                    ProviderName);
            }

            var failureCode = MapLocalFailureCode(
                (int)httpResponse.StatusCode, openAiErrorType, openAiErrorCode);

            _logger.LogWarning(
                "Local provider returned non-success status. ProviderName={ProviderName} " +
                "StatusCode={StatusCode} FailureCode={FailureCode}",
                ProviderName,
                (int)httpResponse.StatusCode,
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
            _logger.LogWarning(
                ex,
                "Local provider response read failed. ProviderName={ProviderName}",
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
            _logger.LogWarning(
                ex,
                "Local provider response JSON parse failed. ProviderName={ProviderName}",
                ProviderName);
            return ProviderFailed("ProviderMalformedResponse", model);
        }

        if (string.IsNullOrWhiteSpace(answerText))
        {
            _logger.LogWarning(
                "Local provider returned empty answer text. ProviderName={ProviderName}",
                ProviderName);
            return ProviderFailed("ProviderMalformedResponse", model);
        }

        // Strip thinking tags before returning — Qwen3 may emit <think>…</think> blocks.
        if (_settings.StripThinking)
        {
            answerText = ThinkTagRegex.Replace(answerText, string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(answerText))
            {
                _logger.LogWarning(
                    "Local provider answer was empty after stripping thinking tags. " +
                    "ProviderName={ProviderName}",
                    ProviderName);
                return ProviderFailed("ProviderMalformedResponse", model);
            }
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

    // Maps an HTTP status and optional parsed error fields to a safe internal failure code.
    // Local runtimes (Ollama, LM Studio) typically do not produce quota/billing errors, so
    // those mappings from the OpenAI provider are omitted here.
    private static string MapLocalFailureCode(int statusCode, string? errorType, string? errorCode)
    {
        // Model not available
        if (IsMatch(errorCode, "model_not_found") || IsMatch(errorCode, "model_decommissioned"))
            return "ProviderModelUnavailable";

        // Invalid request
        if (IsMatch(errorType, "invalid_request_error") || IsMatch(errorCode, "context_length_exceeded"))
            return "ProviderInvalidRequest";

        // HTTP status fallback
        return statusCode switch
        {
            400 or 422 => "ProviderInvalidRequest",
            404 => "ProviderModelUnavailable",
            408 => "ProviderTimeout",
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

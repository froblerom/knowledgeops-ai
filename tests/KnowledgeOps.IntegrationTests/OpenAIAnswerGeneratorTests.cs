using System.Net;
using System.Text;
using System.Text.Json;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Infrastructure.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.IntegrationTests;

public sealed class OpenAIAnswerGeneratorTests
{
    private static readonly string FakeApiKey = "sk-test-fake-key-never-live";

    private static OpenAIAnswerGenerator BuildGenerator(
        HttpMessageHandler handler,
        string? apiKey = null,
        OpenAIAnswerGeneratorSettings? settings = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:OpenAI:ApiKey"] = apiKey ?? FakeApiKey
            })
            .Build();

        return new OpenAIAnswerGenerator(
            new HttpClient(handler),
            Options.Create(settings ?? new OpenAIAnswerGeneratorSettings { TimeoutSeconds = 5 }),
            config,
            NullLogger<OpenAIAnswerGenerator>.Instance);
    }

    private static AuthorizedChunkContext MakeChunk(string text = "Sample chunk content.") =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), text, 0, null, null);

    private static string BuildOpenAiErrorBody(string? type, string? code,
        string message = "OpenAI error.") =>
        JsonSerializer.Serialize(new
        {
            error = new
            {
                message,
                type,
                param = (string?)null,
                code
            }
        });

    private static string BuildSuccessResponse(
        string content = "Grounded answer from OpenAI.",
        int promptTokens = 100,
        int completionTokens = 50) =>
        JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { role = "assistant", content } }
            },
            usage = new { prompt_tokens = promptTokens, completion_tokens = completionTokens, total_tokens = promptTokens + completionTokens }
        });

    [Fact]
    public async Task GenerateAsync_ValidResponse_ReturnsGroundedAnswer()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, BuildSuccessResponse("Policy answer text."));
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "What is the policy?",
                SystemInstruction: "Use only provided context.",
                FormattedContext: "Context chunk here."));

        Assert.Equal(AnswerState.Grounded, result.State);
        Assert.Equal("Policy answer text.", result.AnswerText);
    }

    [Fact]
    public async Task GenerateAsync_MapsInputAndOutputTokens()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, BuildSuccessResponse(promptTokens: 123, completionTokens: 45));
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(123, result.InputTokens);
        Assert.Equal(45, result.OutputTokens);
    }

    [Fact]
    public async Task GenerateAsync_SendsSystemInstructionAsSystemRoleMessage()
    {
        string? capturedBody = null;
        var handler = new CapturingHttpMessageHandler(
            (_, body) => { capturedBody = body; },
            HttpStatusCode.OK,
            BuildSuccessResponse());
        var generator = BuildGenerator(handler);

        await generator.GenerateAsync(new AnswerGenerationRequest(
            [MakeChunk()],
            "What is the policy?",
            SystemInstruction: "Only use provided context.",
            FormattedContext: "The formatted context."));

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody);

        var messages = doc.RootElement.GetProperty("messages");
        var systemMessage = messages.EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("role").GetString() == "system");

        Assert.Equal("Only use provided context.", systemMessage.GetProperty("content").GetString());
    }

    [Fact]
    public async Task GenerateAsync_SendsFormattedContextAndQuestionInUserMessage()
    {
        string? capturedBody = null;
        var handler = new CapturingHttpMessageHandler(
            (_, body) => { capturedBody = body; },
            HttpStatusCode.OK,
            BuildSuccessResponse());
        var generator = BuildGenerator(handler);

        await generator.GenerateAsync(new AnswerGenerationRequest(
            [MakeChunk()],
            "What is the policy?",
            FormattedContext: "My formatted context block."));

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody);

        var messages = doc.RootElement.GetProperty("messages");
        var userMessage = messages.EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("role").GetString() == "user");
        var userContent = userMessage.GetProperty("content").GetString() ?? string.Empty;

        Assert.Contains("My formatted context block.", userContent);
        Assert.Contains("What is the policy?", userContent);
    }

    [Fact]
    public async Task GenerateAsync_401Response_ReturnsProviderFailed_Unauthorized()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, "{}");
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderUnauthorized", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_429Response_ReturnsProviderFailed_RateLimited()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.TooManyRequests, "{}");
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderRateLimited", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_400Response_ReturnsProviderFailed_InvalidRequest()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, "{}");
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderInvalidRequest", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_500Response_ReturnsProviderFailed_Unavailable()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "{}");
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderUnavailable", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_InvalidJson_ReturnsProviderFailed_MalformedResponse()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "not-valid-json");
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderMalformedResponse", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_EmptyChoices_ReturnsProviderFailed_ResponseInvalid()
    {
        var emptyResponse = JsonSerializer.Serialize(new { choices = Array.Empty<object>() });
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, emptyResponse);
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
    }

    [Fact]
    public async Task GenerateAsync_Timeout_ReturnsProviderFailed_Timeout()
    {
        var handler = new TimeoutHttpMessageHandler();
        var generator = BuildGenerator(handler,
            settings: new OpenAIAnswerGeneratorSettings { TimeoutSeconds = 1 });

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderTimeout", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_DoesNotExposeApiKeyInFailureCode()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, "{}");
        var generator = BuildGenerator(handler, apiKey: FakeApiKey);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        // Failure code must never contain or reference the API key
        Assert.DoesNotContain(FakeApiKey, result.SafeFailureCode ?? string.Empty);
    }

    [Fact]
    public void GenerateAsync_ProviderNameIsOpenAI()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var generator = BuildGenerator(handler);

        Assert.Equal("OpenAI", generator.ProviderName);
    }

    // ── Error mapping: quota and rate-limit ───────────────────────────────────

    [Fact]
    public async Task GenerateAsync_429WithInsufficientQuota_ReturnsProviderQuotaExceeded()
    {
        // The real observed failure: OpenAI 429 with insufficient_quota in the body.
        // Must NOT be classified as ProviderRateLimited.
        var body = BuildOpenAiErrorBody("insufficient_quota", "insufficient_quota");
        var handler = new FakeHttpMessageHandler(HttpStatusCode.TooManyRequests, body);
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderQuotaExceeded", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_429WithRateLimitExceeded_ReturnsProviderRateLimited()
    {
        // Generic rate-limit (e.g., requests-per-minute) must remain ProviderRateLimited.
        var body = BuildOpenAiErrorBody("rate_limit_exceeded", "rate_limit_exceeded");
        var handler = new FakeHttpMessageHandler(HttpStatusCode.TooManyRequests, body);
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderRateLimited", result.SafeFailureCode);
    }

    // ── Error mapping: auth ───────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_401WithInvalidApiKeyCode_ReturnsProviderUnauthorized()
    {
        var body = BuildOpenAiErrorBody("invalid_request_error", "invalid_api_key");
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, body);
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderUnauthorized", result.SafeFailureCode);
    }

    // ── Error mapping: invalid request ────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_400WithInvalidRequestErrorType_ReturnsProviderInvalidRequest()
    {
        var body = BuildOpenAiErrorBody("invalid_request_error", null);
        var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, body);
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderInvalidRequest", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_400WithContextLengthExceeded_ReturnsProviderInvalidRequest()
    {
        var body = BuildOpenAiErrorBody("invalid_request_error", "context_length_exceeded");
        var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, body);
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderInvalidRequest", result.SafeFailureCode);
    }

    // ── Error mapping: model unavailable ─────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_404WithModelNotFound_ReturnsProviderModelUnavailable()
    {
        var body = BuildOpenAiErrorBody("invalid_request_error", "model_not_found");
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, body);
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderModelUnavailable", result.SafeFailureCode);
    }

    // ── Error mapping: server errors ──────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_503Response_ReturnsProviderUnavailable()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "{}");
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderUnavailable", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_500WithServerErrorType_ReturnsProviderUnavailable()
    {
        var body = BuildOpenAiErrorBody("server_error", "server_error");
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, body);
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderUnavailable", result.SafeFailureCode);
    }

    // ── Error mapping: unknown / fallback ─────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_UnknownErrorTypeAndCode_ReturnsFallbackProviderFailed()
    {
        // An unrecognized error type/code with an unusual HTTP status falls through
        // to the generic ProviderFailed code.
        var body = BuildOpenAiErrorBody("completely_unknown_error_type", "completely_unknown_code");
        var handler = new FakeHttpMessageHandler((HttpStatusCode)418, body);
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderFailed", result.SafeFailureCode);
    }

    // ── Security: raw error body not leaked ───────────────────────────────────

    [Fact]
    public async Task GenerateAsync_RawOpenAiErrorMessageNotInFailureCode()
    {
        // The raw error message from OpenAI may contain billing/account details.
        // It must never appear in the safe failure code stored in the database.
        const string rawMessage =
            "You exceeded your current quota, please check your plan and billing details.";
        var body = JsonSerializer.Serialize(new
        {
            error = new
            {
                message = rawMessage,
                type = "insufficient_quota",
                code = "insufficient_quota"
            }
        });
        var handler = new FakeHttpMessageHandler(HttpStatusCode.TooManyRequests, body);
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderQuotaExceeded", result.SafeFailureCode);
        // Raw message must not surface anywhere in the result
        Assert.DoesNotContain(rawMessage, result.SafeFailureCode ?? string.Empty);
        Assert.DoesNotContain(rawMessage, result.AnswerText ?? string.Empty);
    }

    // ── Fake HTTP handlers ─────────────────────────────────────────────────────

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
    }

    // Captures request body as string before HttpContent is disposed
    private sealed class CapturingHttpMessageHandler(
        Action<HttpRequestMessage, string> onRequest,
        HttpStatusCode statusCode,
        string responseBody)
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var bodyString = request.Content is not null
                ? await request.Content.ReadAsStringAsync(cancellationToken)
                : string.Empty;

            onRequest(request, bodyString);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class TimeoutHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
            throw new TaskCanceledException("Simulated timeout.");
        }
    }
}

using System.Net;
using System.Text;
using System.Text.Json;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Infrastructure.Chat;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.IntegrationTests;

public sealed class LocalOpenAICompatibleAnswerGeneratorTests
{
    private static readonly string FakeApiKey = "local-test-fake-key-never-live";

    private static LocalOpenAICompatibleAnswerGenerator BuildGenerator(
        HttpMessageHandler handler,
        LocalOpenAICompatibleAnswerGeneratorSettings? settings = null)
    {
        return new LocalOpenAICompatibleAnswerGenerator(
            new HttpClient(handler),
            Options.Create(settings ?? DefaultSettings()),
            NullLogger<LocalOpenAICompatibleAnswerGenerator>.Instance);
    }

    private static LocalOpenAICompatibleAnswerGeneratorSettings DefaultSettings(
        string? apiKey = null,
        bool stripThinking = true,
        string baseUrl = "http://localhost:11434/v1",
        string model = "qwen3:8b",
        string providerName = "QwenLocal",
        int timeoutSeconds = 5) =>
        new()
        {
            ProviderName = providerName,
            BaseUrl = baseUrl,
            ApiKey = apiKey,
            Model = model,
            MaxTokens = 600,
            Temperature = 0.1,
            TimeoutSeconds = timeoutSeconds,
            StripThinking = stripThinking
        };

    private static AuthorizedChunkContext MakeChunk(string text = "Sample chunk content.") =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), text, 0, null, null);

    private static string BuildSuccessResponse(
        string content = "Grounded answer from local model.",
        int promptTokens = 80,
        int completionTokens = 40) =>
        JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { role = "assistant", content } }
            },
            usage = new
            {
                prompt_tokens = promptTokens,
                completion_tokens = completionTokens,
                total_tokens = promptTokens + completionTokens
            }
        });

    // ── Success path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_ValidResponse_ReturnsGroundedAnswer()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            BuildSuccessResponse("Policy answer from local model."));
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "What is the policy?",
                SystemInstruction: "Use only provided context.",
                FormattedContext: "Context chunk here."));

        Assert.Equal(AnswerState.Grounded, result.State);
        Assert.Equal("Policy answer from local model.", result.AnswerText);
    }

    [Fact]
    public async Task GenerateAsync_SendsCorrectModelAndMessages()
    {
        string? capturedBody = null;
        var handler = new CapturingHttpMessageHandler(
            (_, body) => { capturedBody = body; },
            HttpStatusCode.OK,
            BuildSuccessResponse());
        var generator = BuildGenerator(handler, DefaultSettings(model: "qwen3:8b"));

        await generator.GenerateAsync(new AnswerGenerationRequest(
            [MakeChunk()],
            "What is the policy?",
            SystemInstruction: "Only use provided context.",
            FormattedContext: "The formatted context."));

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody);

        Assert.Equal("qwen3:8b", doc.RootElement.GetProperty("model").GetString());

        var messages = doc.RootElement.GetProperty("messages");
        Assert.True(messages.GetArrayLength() >= 2);

        var systemMsg = messages.EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("role").GetString() == "system");
        Assert.Equal("Only use provided context.", systemMsg.GetProperty("content").GetString());

        var userMsg = messages.EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("role").GetString() == "user");
        var userContent = userMsg.GetProperty("content").GetString() ?? string.Empty;
        Assert.Contains("The formatted context.", userContent);
        Assert.Contains("What is the policy?", userContent);
    }

    [Fact]
    public async Task GenerateAsync_UsesConfiguredBaseUrl()
    {
        string? capturedUri = null;
        var handler = new CapturingHttpMessageHandler(
            (req, _) => { capturedUri = req.RequestUri?.ToString(); },
            HttpStatusCode.OK,
            BuildSuccessResponse());
        var generator = BuildGenerator(handler,
            DefaultSettings(baseUrl: "http://localhost:11434/v1"));

        await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.NotNull(capturedUri);
        Assert.Contains("localhost:11434", capturedUri);
        Assert.Contains("/chat/completions", capturedUri);
    }

    [Fact]
    public async Task GenerateAsync_NoAuthHeaderWhenApiKeyEmpty()
    {
        string? capturedAuthHeader = null;
        var handler = new CapturingHttpMessageHandler(
            (req, _) =>
            {
                capturedAuthHeader = req.Headers.Authorization?.ToString();
            },
            HttpStatusCode.OK,
            BuildSuccessResponse());
        // apiKey = null
        var generator = BuildGenerator(handler, DefaultSettings(apiKey: null));

        await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Null(capturedAuthHeader);
    }

    [Fact]
    public async Task GenerateAsync_SendsAuthHeaderWhenApiKeySet()
    {
        string? capturedAuthHeader = null;
        var handler = new CapturingHttpMessageHandler(
            (req, _) =>
            {
                capturedAuthHeader = req.Headers.Authorization?.ToString();
            },
            HttpStatusCode.OK,
            BuildSuccessResponse());
        var generator = BuildGenerator(handler, DefaultSettings(apiKey: "lm-studio"));

        await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal("Bearer lm-studio", capturedAuthHeader);
    }

    // ── Thinking tag stripping ────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_StripThinking_RemovesThinkTags()
    {
        const string raw = "<think>\nhidden internal reasoning\n</think>\n\nFinal grounded answer.";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, BuildSuccessResponse(raw));
        var generator = BuildGenerator(handler, DefaultSettings(stripThinking: true));

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.Grounded, result.State);
        Assert.Equal("Final grounded answer.", result.AnswerText);
        Assert.DoesNotContain("<think>", result.AnswerText ?? string.Empty);
        Assert.DoesNotContain("hidden internal reasoning", result.AnswerText ?? string.Empty);
    }

    [Fact]
    public async Task GenerateAsync_StripThinking_ContentWithoutTagsUnchanged()
    {
        const string content = "Plain answer with no thinking tags.";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, BuildSuccessResponse(content));
        var generator = BuildGenerator(handler, DefaultSettings(stripThinking: true));

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.Grounded, result.State);
        Assert.Equal(content, result.AnswerText);
    }

    [Fact]
    public async Task GenerateAsync_StripThinkingFalse_PreservesThinkTags()
    {
        const string raw = "<think>reasoning</think>Answer with tags preserved.";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, BuildSuccessResponse(raw));
        var generator = BuildGenerator(handler, DefaultSettings(stripThinking: false));

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.Grounded, result.State);
        Assert.Equal(raw, result.AnswerText);
        Assert.Contains("<think>", result.AnswerText ?? string.Empty);
    }

    [Fact]
    public async Task GenerateAsync_ThinkingOnlyContent_ReturnsProviderMalformedResponse()
    {
        // After stripping, the answer is empty — not safe to return.
        const string thinkingOnly = "<think>\nOnly internal reasoning, no visible answer.\n</think>";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, BuildSuccessResponse(thinkingOnly));
        var generator = BuildGenerator(handler, DefaultSettings(stripThinking: true));

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderMalformedResponse", result.SafeFailureCode);
    }

    // ── Ollama-style reasoning field (content separate from reasoning) ────────

    [Fact]
    public async Task GenerateAsync_OllamaStyleEmptyContent_ReturnsProviderMalformedResponse()
    {
        // Ollama's /v1/chat/completions puts CoT in "reasoning" and the answer in "content".
        // When max_tokens is exhausted during thinking, content="" — generator must reject it.
        var ollamaResponse = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        role = "assistant",
                        content = "",
                        reasoning = "Okay, the user wants me to... [exhausted token budget here]"
                    },
                    finish_reason = "length"
                }
            },
            usage = new { prompt_tokens = 123, completion_tokens = 600, total_tokens = 723 }
        });
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, ollamaResponse);
        var generator = BuildGenerator(handler, DefaultSettings(stripThinking: true));

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk("Policy text here.")], "What is the refund policy?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderMalformedResponse", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_OllamaStyleNonEmptyContent_ReturnsGroundedAnswer()
    {
        // Ollama normal success: reasoning in "reasoning" field, visible answer in "content".
        // The generator should use content and ignore the reasoning field entirely.
        var ollamaResponse = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        role = "assistant",
                        content = "Before offering a refund, verify proof of damage and the original receipt.",
                        reasoning = "Let me think through the policy carefully..."
                    },
                    finish_reason = "stop"
                }
            },
            usage = new { prompt_tokens = 123, completion_tokens = 105, total_tokens = 228 }
        });
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, ollamaResponse);
        var generator = BuildGenerator(handler, DefaultSettings(stripThinking: true));

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk("Policy text here.")], "What is the refund policy?"));

        Assert.Equal(AnswerState.Grounded, result.State);
        Assert.Equal(
            "Before offering a refund, verify proof of damage and the original receipt.",
            result.AnswerText);
        Assert.Equal(105, result.OutputTokens);
    }

    // ── Error mapping ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_ConnectionFailure_ReturnsProviderUnavailable()
    {
        var handler = new ThrowingHttpMessageHandler(
            new HttpRequestException("Connection refused."));
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderUnavailable", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_Timeout_ReturnsProviderTimeout()
    {
        var handler = new TimeoutHttpMessageHandler();
        var generator = BuildGenerator(handler,
            DefaultSettings(timeoutSeconds: 1));

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderTimeout", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_404Response_ReturnsProviderModelUnavailable()
    {
        // Ollama returns 404 when the model has not been pulled.
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound,
            JsonSerializer.Serialize(new { error = "model 'qwen3:8b' not found, try pulling it first" }));
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderModelUnavailable", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_400Response_ReturnsProviderInvalidRequest()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, "{}");
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderInvalidRequest", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_429Response_ReturnsProviderRateLimited()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.TooManyRequests, "{}");
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderRateLimited", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_500Response_ReturnsProviderUnavailable()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "{}");
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderUnavailable", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_InvalidJson_ReturnsProviderMalformedResponse()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "not-valid-json");
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderMalformedResponse", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_EmptyChoices_ReturnsProviderMalformedResponse()
    {
        var emptyResponse = JsonSerializer.Serialize(new { choices = Array.Empty<object>() });
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, emptyResponse);
        var generator = BuildGenerator(handler);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Equal("ProviderMalformedResponse", result.SafeFailureCode);
    }

    // ── Security ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_ApiKeyNotInResult()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, "{}");
        var generator = BuildGenerator(handler, DefaultSettings(apiKey: FakeApiKey));

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([MakeChunk()], "Question?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.DoesNotContain(FakeApiKey, result.SafeFailureCode ?? string.Empty);
        Assert.DoesNotContain(FakeApiKey, result.AnswerText ?? string.Empty);
    }

    // ── Provider identity ─────────────────────────────────────────────────────

    [Fact]
    public void ProviderName_UsesConfiguredProviderName()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var generator = BuildGenerator(handler, DefaultSettings(providerName: "QwenLocal"));

        Assert.Equal("QwenLocal", generator.ProviderName);
        Assert.Equal("qwen3:8b", generator.DefaultModelName);
    }

    // ── Fake HTTP handlers ────────────────────────────────────────────────────

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

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw exception;
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

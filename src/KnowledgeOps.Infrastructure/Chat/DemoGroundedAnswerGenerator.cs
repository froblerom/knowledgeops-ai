using KnowledgeOps.Application.Chat;
using KnowledgeOps.Domain.Chat;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.Infrastructure.Chat;

/// <summary>
/// Deterministic, extractive answer generator for local development, CI, and portfolio demo.
/// Uses retrieved chunk text directly — no external API, no API key, no network calls.
/// Produces a professional grounded answer by quoting authorized retrieved context.
/// </summary>
internal sealed class DemoGroundedAnswerGenerator(
    IOptions<DemoGroundedAnswerGeneratorSettings> options) : IAiAnswerGenerator
{
    private const int MaxCharsPerChunk = 1000;
    private const int MaxChunksToInclude = 2;

    private readonly DemoGroundedAnswerGeneratorSettings _settings = options.Value;

    public string ProviderName => _settings.ProviderName;
    public string DefaultModelName => _settings.ModelName;

    public Task<AnswerGenerationResult> GenerateAsync(
        AnswerGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var chunks = request.AuthorizedChunks.Take(MaxChunksToInclude).ToList();

        var parts = new List<string>(chunks.Count);
        foreach (var chunk in chunks)
        {
            var text = chunk.ChunkText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (text.Length > MaxCharsPerChunk)
                text = text[..MaxCharsPerChunk].TrimEnd() + "...";

            var part = string.IsNullOrWhiteSpace(chunk.SectionLabel)
                ? text
                : $"**{chunk.SectionLabel}**\n\n{text}";

            parts.Add(part);
        }

        if (parts.Count == 0)
        {
            return Task.FromResult(new AnswerGenerationResult(
                State: AnswerState.ProviderFailed,
                AnswerText: null,
                InputTokens: null,
                OutputTokens: null,
                ModelUsed: _settings.ModelName,
                ProviderName: _settings.ProviderName,
                SafeFailureCode: "NoUsableChunkText"));
        }

        var answerText =
            "Based on the available knowledge base, here is the relevant guidance:\n\n" +
            string.Join("\n\n", parts) +
            "\n\nPlease verify this information with the cited source and your supervisor before taking action.";

        return Task.FromResult(new AnswerGenerationResult(
            State: AnswerState.Grounded,
            AnswerText: answerText,
            InputTokens: null,
            OutputTokens: null,
            ModelUsed: _settings.ModelName,
            ProviderName: _settings.ProviderName,
            SafeFailureCode: null));
    }
}

using System.Security.Cryptography;
using System.Text;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Domain.Chat;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class FakeAnswerGenerator(IOptions<FakeAnswerGeneratorSettings> options) : IAiAnswerGenerator
{
    private readonly FakeAnswerGeneratorSettings _settings = options.Value;

    public string ProviderName => _settings.ProviderName;
    public string DefaultModelName => _settings.ModelName;

    public Task<AnswerGenerationResult> GenerateAsync(
        AnswerGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var candidateCount = request.AuthorizedChunks.Count;
        var input = Encoding.UTF8.GetBytes($"{_settings.ProviderName}|{request.UserQuestion}|{candidateCount}");
        _ = SHA256.HashData(input);

        var result = new AnswerGenerationResult(
            State: AnswerState.Grounded,
            AnswerText: $"Fake grounded answer based on {candidateCount} authorized retrieved context(s).",
            InputTokens: null,
            OutputTokens: null,
            ModelUsed: _settings.ModelName,
            ProviderName: _settings.ProviderName,
            SafeFailureCode: null);

        return Task.FromResult(result);
    }
}

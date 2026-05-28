namespace KnowledgeOps.Application.Chat;

public interface IAiAnswerGenerator
{
    string ProviderName { get; }
    string DefaultModelName { get; }

    Task<AnswerGenerationResult> GenerateAsync(
        AnswerGenerationRequest request,
        CancellationToken cancellationToken = default);
}

namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class FakeAnswerGeneratorSettings
{
    public string ProviderName { get; init; } = "Fake";
    public string ModelName { get; init; } = "fake-answer-v1";
}

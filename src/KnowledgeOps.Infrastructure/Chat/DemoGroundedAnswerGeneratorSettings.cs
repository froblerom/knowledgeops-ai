namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class DemoGroundedAnswerGeneratorSettings
{
    public string ProviderName { get; init; } = "Demo";
    public string ModelName { get; init; } = "demo-extractive-v1";
}

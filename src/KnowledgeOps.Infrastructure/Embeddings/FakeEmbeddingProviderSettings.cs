namespace KnowledgeOps.Infrastructure.Embeddings;

internal sealed class FakeEmbeddingProviderSettings
{
    public string ProviderName { get; init; } = "Fake";
    public string ModelName { get; init; } = "fake-deterministic-v1";
    public int Dimensions { get; init; } = 16;
}

namespace KnowledgeOps.Application.Embeddings;

public interface IEmbeddingProvider
{
    string ProviderName { get; }
    string DefaultModelName { get; }
    int DefaultDimensions { get; }

    Task<EmbeddingResponse> GenerateAsync(EmbeddingRequest request, CancellationToken cancellationToken = default);
}

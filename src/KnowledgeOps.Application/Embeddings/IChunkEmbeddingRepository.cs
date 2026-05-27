using KnowledgeOps.Domain.Documents;

namespace KnowledgeOps.Application.Embeddings;

public sealed record ChunkEmbeddingRecord(
    Guid EmbeddingId,
    Guid ChunkId,
    Guid OrganizationId,
    string ProviderName,
    string ModelName,
    string? VectorData,
    int? VectorDimensions,
    EmbeddingStatus Status,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public interface IChunkEmbeddingRepository
{
    Task SaveEmbeddingsAsync(
        IReadOnlyList<ChunkEmbeddingRecord> embeddings,
        CancellationToken cancellationToken = default);
}

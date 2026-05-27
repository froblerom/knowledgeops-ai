using KnowledgeOps.Application.Embeddings;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Infrastructure.Persistence;

namespace KnowledgeOps.Infrastructure.Embeddings;

internal sealed class EfChunkEmbeddingRepository(KnowledgeOpsDbContext dbContext) : IChunkEmbeddingRepository
{
    public async Task SaveEmbeddingsAsync(
        IReadOnlyList<ChunkEmbeddingRecord> embeddings,
        CancellationToken cancellationToken = default)
    {
        var entities = embeddings.Select(e => new ChunkEmbedding
        {
            Id = e.EmbeddingId,
            ChunkId = e.ChunkId,
            OrganizationId = e.OrganizationId,
            ProviderName = e.ProviderName,
            ModelName = e.ModelName,
            VectorData = e.VectorData,
            VectorDimensions = e.VectorDimensions,
            Status = e.Status,
            FailureReason = e.FailureReason,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        });

        dbContext.ChunkEmbeddings.AddRange(entities);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

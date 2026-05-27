using KnowledgeOps.Application.Retrieval;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Retrieval;

internal sealed class LocalRetrievalStorageHealthCheck(KnowledgeOpsDbContext dbContext)
    : IRetrievalStorageHealthCheck
{
    public async Task<RetrievalStorageHealthResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var indexedCount = await dbContext.ChunkEmbeddings
                .AsNoTracking()
                .CountAsync(e => e.IndexStatus == EmbeddingIndexStatus.Indexed, cancellationToken);
            var failedCount = await dbContext.ChunkEmbeddings
                .AsNoTracking()
                .CountAsync(e => e.IndexStatus == EmbeddingIndexStatus.Failed, cancellationToken);

            return new RetrievalStorageHealthResult(
                IsHealthy: true,
                ProviderName: LocalVectorStore.ProviderName,
                IndexedEmbeddingCount: indexedCount,
                FailedIndexCount: failedCount);
        }
        catch
        {
            return new RetrievalStorageHealthResult(
                IsHealthy: false,
                ProviderName: LocalVectorStore.ProviderName,
                IndexedEmbeddingCount: 0,
                FailedIndexCount: 0,
                DegradedReason: "Retrieval storage is unavailable.");
        }
    }
}

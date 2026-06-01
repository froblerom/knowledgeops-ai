using KnowledgeOps.Application.Retrieval;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Retrieval;

internal sealed class EfRetrievalEligibilityRepository(KnowledgeOpsDbContext dbContext)
    : IRetrievalEligibilityRepository
{
    public async Task<IReadOnlyList<RetrievalEligibleCandidateIdentity>> RevalidateAsync(
        Guid organizationId,
        IReadOnlyList<RetrievalCandidateIdentity> candidates,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty || candidates.Count == 0)
            return [];

        var candidateKeys = candidates
            .Where(candidate => candidate.OrganizationId == organizationId)
            .Distinct()
            .Select(candidate => (
                candidate.OrganizationId,
                candidate.DocumentId,
                candidate.ChunkId,
                candidate.ChunkEmbeddingId))
            .ToHashSet();

        if (candidateKeys.Count == 0)
            return [];

        var eligibleRows = await (
            from embedding in dbContext.ChunkEmbeddings.AsNoTracking()
            join chunk in dbContext.DocumentChunks.AsNoTracking() on embedding.ChunkId equals chunk.Id
            join document in dbContext.Documents.AsNoTracking() on chunk.DocumentId equals document.Id
            where embedding.OrganizationId == organizationId
                && embedding.Status == EmbeddingStatus.Ready
                && embedding.IndexStatus == EmbeddingIndexStatus.Indexed
                && chunk.OrganizationId == organizationId
                && chunk.DeletedAt == null
                && document.OrganizationId == organizationId
                && document.ProcessingStatus == DocumentProcessingStatus.Processed
                && document.IsRetrievalEnabled
                && document.DeletedAt == null
            select new RetrievalEligibleCandidateIdentity(
                embedding.OrganizationId,
                document.Id,
                chunk.Id,
                embedding.Id))
            .ToListAsync(cancellationToken);

        return eligibleRows
            .Where(row => candidateKeys.Contains((
                row.OrganizationId,
                row.DocumentId,
                row.ChunkId,
                row.ChunkEmbeddingId)))
            .ToArray();
    }
}

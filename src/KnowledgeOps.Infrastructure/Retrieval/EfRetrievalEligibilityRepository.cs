using System.Linq.Expressions;
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

        var identityPredicate = BuildIdentityPredicate(organizationId, candidates);

        return await (
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
            .Where(identityPredicate)
            .ToListAsync(cancellationToken);
    }

    private static Expression<Func<RetrievalEligibleCandidateIdentity, bool>> BuildIdentityPredicate(
        Guid organizationId,
        IReadOnlyList<RetrievalCandidateIdentity> candidates)
    {
        var row = Expression.Parameter(typeof(RetrievalEligibleCandidateIdentity), "row");
        Expression? body = null;

        foreach (var candidate in candidates.Distinct())
        {
            if (candidate.OrganizationId != organizationId)
                continue;

            var organizationMatches = Expression.Equal(
                Expression.Property(row, nameof(RetrievalEligibleCandidateIdentity.OrganizationId)),
                Expression.Constant(candidate.OrganizationId));
            var documentMatches = Expression.Equal(
                Expression.Property(row, nameof(RetrievalEligibleCandidateIdentity.DocumentId)),
                Expression.Constant(candidate.DocumentId));
            var chunkMatches = Expression.Equal(
                Expression.Property(row, nameof(RetrievalEligibleCandidateIdentity.ChunkId)),
                Expression.Constant(candidate.ChunkId));
            var embeddingMatches = Expression.Equal(
                Expression.Property(row, nameof(RetrievalEligibleCandidateIdentity.ChunkEmbeddingId)),
                Expression.Constant(candidate.ChunkEmbeddingId));

            var identityMatches = Expression.AndAlso(
                organizationMatches,
                Expression.AndAlso(
                    Expression.AndAlso(documentMatches, chunkMatches),
                    embeddingMatches));

            body = body is null ? identityMatches : Expression.OrElse(body, identityMatches);
        }

        return Expression.Lambda<Func<RetrievalEligibleCandidateIdentity, bool>>(
            body ?? Expression.Constant(false),
            row);
    }
}

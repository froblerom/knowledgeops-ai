namespace KnowledgeOps.Application.Retrieval;

public interface IRetrievalEligibilityRepository
{
    Task<IReadOnlyList<RetrievalEligibleCandidateIdentity>> RevalidateAsync(
        Guid organizationId,
        IReadOnlyList<RetrievalCandidateIdentity> candidates,
        CancellationToken cancellationToken = default);
}

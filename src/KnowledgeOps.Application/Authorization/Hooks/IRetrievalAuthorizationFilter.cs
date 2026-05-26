namespace KnowledgeOps.Application.Authorization.Hooks;

// Future contract: applied before retrieval candidate selection.
// Implementations must exclude documents outside the user's organization scope.
// Do NOT implement retrieval, vector search, or embeddings here.
// Sprint 16+ implements retrieval workflows using this contract.
public interface IRetrievalAuthorizationFilter
{
    bool IsDocumentEligibleForRetrieval(Guid documentOrganizationId, Guid currentUserOrganizationId);
}

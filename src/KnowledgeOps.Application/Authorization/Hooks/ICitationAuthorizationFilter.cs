namespace KnowledgeOps.Application.Authorization.Hooks;

// Future contract: applied before citations are exposed in chat responses.
// Implementations must exclude citations referencing documents outside the user's organization scope.
// Do NOT implement citation storage, retrieval, or UI here.
// Sprint 19+ implements citation workflows using this contract.
public interface ICitationAuthorizationFilter
{
    bool IsCitationAuthorizedForUser(Guid citationOrganizationId, Guid currentUserOrganizationId);
}

namespace KnowledgeOps.Application.Authorization;

public interface IOrganizationScopeService
{
    // Returns a result describing whether the current user's organization matches the target.
    // Guid.Empty for either argument denies access and returns a specific failure reason.
    // Admin is NOT exempt — same-organization rule applies to all roles.
    OrganizationScopeResult CheckScope(Guid currentUserOrganizationId, Guid targetOrganizationId);

    bool IsInScope(Guid currentUserOrganizationId, Guid targetOrganizationId);
}

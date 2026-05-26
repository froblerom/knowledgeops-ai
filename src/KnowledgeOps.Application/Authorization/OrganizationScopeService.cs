namespace KnowledgeOps.Application.Authorization;

// Pure equality implementation. No DbContext dependency.
// All roles, including Admin, are subject to the same-organization rule (ADR-010).
public sealed class OrganizationScopeService : IOrganizationScopeService
{
    public OrganizationScopeResult CheckScope(Guid currentUserOrganizationId, Guid targetOrganizationId)
    {
        if (currentUserOrganizationId == Guid.Empty)
            return OrganizationScopeResult.Denied(AuthorizationFailureReason.MissingOrganization);

        if (targetOrganizationId == Guid.Empty)
            return OrganizationScopeResult.Denied(AuthorizationFailureReason.MissingTargetOrganization);

        if (currentUserOrganizationId != targetOrganizationId)
            return OrganizationScopeResult.Denied(AuthorizationFailureReason.CrossOrganizationAccess);

        return OrganizationScopeResult.Allowed();
    }

    public bool IsInScope(Guid currentUserOrganizationId, Guid targetOrganizationId) =>
        CheckScope(currentUserOrganizationId, targetOrganizationId).IsAllowed;
}

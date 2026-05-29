namespace KnowledgeOps.Application.Authorization.Hooks;

internal sealed class DefaultCitationAuthorizationFilter : ICitationAuthorizationFilter
{
    public bool IsCitationAuthorizedForUser(Guid citationOrganizationId, Guid currentUserOrganizationId)
        => citationOrganizationId == currentUserOrganizationId;
}

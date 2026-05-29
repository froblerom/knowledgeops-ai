namespace KnowledgeOps.Application.Authorization.Hooks;

internal sealed class DefaultPromptAuthorizationFilter : IPromptAuthorizationFilter
{
    public bool IsChunkAuthorizedForPrompt(Guid chunkOrganizationId, Guid currentUserOrganizationId)
        => chunkOrganizationId == currentUserOrganizationId;
}

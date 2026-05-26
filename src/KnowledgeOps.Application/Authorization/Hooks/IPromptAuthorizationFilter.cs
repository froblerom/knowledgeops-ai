namespace KnowledgeOps.Application.Authorization.Hooks;

// Future contract: applied before retrieved chunks are assembled into a RAG prompt.
// Implementations must exclude chunks from documents outside the user's organization scope.
// Do NOT implement prompt construction or AI provider calls here.
// Sprint 16+ implements prompt workflows using this contract.
public interface IPromptAuthorizationFilter
{
    bool IsChunkAuthorizedForPrompt(Guid chunkOrganizationId, Guid currentUserOrganizationId);
}

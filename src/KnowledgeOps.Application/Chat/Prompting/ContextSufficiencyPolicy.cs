using KnowledgeOps.Application.Chat;

namespace KnowledgeOps.Application.Chat.Prompting;

internal sealed class ContextSufficiencyPolicy : IContextSufficiencyPolicy
{
    public ContextSufficiencyResult Evaluate(IReadOnlyList<AuthorizedChunkContext> authorizedChunks)
    {
        if (authorizedChunks == null || authorizedChunks.Count == 0)
            return new ContextSufficiencyResult(false, "NoAuthorizedChunks");

        return new ContextSufficiencyResult(true, null);
    }
}

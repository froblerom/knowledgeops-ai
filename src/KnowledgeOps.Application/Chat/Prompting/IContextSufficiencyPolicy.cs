using KnowledgeOps.Application.Chat;

namespace KnowledgeOps.Application.Chat.Prompting;

public interface IContextSufficiencyPolicy
{
    ContextSufficiencyResult Evaluate(IReadOnlyList<AuthorizedChunkContext> authorizedChunks);
}

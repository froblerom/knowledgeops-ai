using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Application.Chat;

public interface IChatSessionRepository
{
    Task<ChatSession?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ChatSession session, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Application.Chat;

public interface IChatInteractionRepository
{
    Task<ChatInteraction?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ChatInteraction>> GetBySessionIdAsync(Guid sessionId, Guid organizationId, CancellationToken ct = default);
    Task AddAsync(ChatInteraction interaction, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Application.Chat;

public interface IChatSessionRepository
{
    Task<ChatSession?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<ChatSession?> FindByIdAndOrganizationAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<ChatSession>> GetRecentByUserAsync(Guid userId, Guid organizationId, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<ChatSession>> GetRecentByOrganizationAsync(Guid organizationId, int limit, CancellationToken ct = default);
    Task<int> CountInteractionsBySessionAsync(Guid sessionId, CancellationToken ct = default);
    Task AddAsync(ChatSession session, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

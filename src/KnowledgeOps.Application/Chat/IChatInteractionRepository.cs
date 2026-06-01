using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Application.Chat;

public interface IChatInteractionRepository
{
    /// <summary>
    /// Finds a chat interaction by ID within the specified organization.
    /// Returns null when the interaction does not exist or belongs to a different organization.
    /// Both id and organizationId are required for defense-in-depth against cross-org disclosure.
    /// </summary>
    Task<ChatInteraction?> FindByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<ChatInteraction>> GetBySessionIdAsync(Guid sessionId, Guid organizationId, CancellationToken ct = default);
    Task AddAsync(ChatInteraction interaction, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

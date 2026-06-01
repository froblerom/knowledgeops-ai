using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Application.Chat.Citations;

public interface ICitationRepository
{
    Task<IReadOnlyList<Citation>> GetByInteractionIdAsync(Guid interactionId, Guid organizationId, CancellationToken ct = default);
    Task AddRangeAsync(IReadOnlyList<Citation> citations, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

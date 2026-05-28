using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Application.Chat;

public interface IChatInteractionRepository
{
    Task AddAsync(ChatInteraction interaction, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

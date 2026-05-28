using KnowledgeOps.Application.Chat;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Infrastructure.Persistence;

namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class EfChatInteractionRepository(KnowledgeOpsDbContext db) : IChatInteractionRepository
{
    public async Task AddAsync(ChatInteraction interaction, CancellationToken ct = default) =>
        await db.ChatInteractions.AddAsync(interaction, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

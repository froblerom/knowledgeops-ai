using KnowledgeOps.Application.Chat;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class EfChatInteractionRepository(KnowledgeOpsDbContext db) : IChatInteractionRepository
{
    public Task<ChatInteraction?> FindByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default) =>
        db.ChatInteractions.FirstOrDefaultAsync(
            i => i.Id == id && i.OrganizationId == organizationId, ct);

    public async Task<IReadOnlyList<ChatInteraction>> GetBySessionIdAsync(
        Guid sessionId, Guid organizationId, CancellationToken ct = default) =>
        await db.ChatInteractions
            .Where(i => i.ChatSessionId == sessionId && i.OrganizationId == organizationId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(ChatInteraction interaction, CancellationToken ct = default) =>
        await db.ChatInteractions.AddAsync(interaction, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

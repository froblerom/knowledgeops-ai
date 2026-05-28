using KnowledgeOps.Application.Chat;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class EfChatSessionRepository(KnowledgeOpsDbContext db) : IChatSessionRepository
{
    public Task<ChatSession?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ChatSessions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(ChatSession session, CancellationToken ct = default) =>
        await db.ChatSessions.AddAsync(session, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

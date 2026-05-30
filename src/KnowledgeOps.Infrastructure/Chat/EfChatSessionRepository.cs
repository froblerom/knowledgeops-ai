using KnowledgeOps.Application.Chat;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class EfChatSessionRepository(KnowledgeOpsDbContext db) : IChatSessionRepository
{
    public Task<ChatSession?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ChatSessions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<ChatSession?> FindByIdAndOrganizationAsync(
        Guid id, Guid organizationId, CancellationToken ct = default) =>
        db.ChatSessions
            .Where(s => s.Id == id && s.OrganizationId == organizationId && s.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ChatSession>> GetRecentByUserAsync(
        Guid userId, Guid organizationId, int limit, CancellationToken ct = default) =>
        await db.ChatSessions
            .Where(s => s.UserId == userId && s.OrganizationId == organizationId && s.DeletedAt == null)
            .OrderByDescending(s => (DateTimeOffset?)(s.LastInteractionAt ?? s.CreatedAt))
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ChatSession>> GetRecentByOrganizationAsync(
        Guid organizationId, int limit, CancellationToken ct = default) =>
        await db.ChatSessions
            .Where(s => s.OrganizationId == organizationId && s.DeletedAt == null)
            .OrderByDescending(s => (DateTimeOffset?)(s.LastInteractionAt ?? s.CreatedAt))
            .Take(limit)
            .ToListAsync(ct);

    public Task<int> CountInteractionsBySessionAsync(Guid sessionId, CancellationToken ct = default) =>
        db.ChatInteractions.CountAsync(i => i.ChatSessionId == sessionId, ct);

    public async Task AddAsync(ChatSession session, CancellationToken ct = default) =>
        await db.ChatSessions.AddAsync(session, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

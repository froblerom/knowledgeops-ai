using KnowledgeOps.Application.Chat.Citations;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class EfCitationRepository(KnowledgeOpsDbContext db) : ICitationRepository
{
    public async Task<IReadOnlyList<Citation>> GetByInteractionIdAsync(
        Guid interactionId, Guid organizationId, CancellationToken ct = default) =>
        await db.Citations
            .Where(c => c.ChatInteractionId == interactionId && c.OrganizationId == organizationId)
            .OrderBy(c => c.Rank)
            .ToListAsync(ct);

    public async Task AddRangeAsync(IReadOnlyList<Citation> citations, CancellationToken ct = default)
    {
        foreach (var citation in citations)
            await db.Citations.AddAsync(citation, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

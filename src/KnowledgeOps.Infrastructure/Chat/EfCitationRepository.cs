using KnowledgeOps.Application.Chat.Citations;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Infrastructure.Persistence;

namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class EfCitationRepository(KnowledgeOpsDbContext db) : ICitationRepository
{
    public async Task AddRangeAsync(IReadOnlyList<Citation> citations, CancellationToken ct = default)
    {
        foreach (var citation in citations)
            await db.Citations.AddAsync(citation, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

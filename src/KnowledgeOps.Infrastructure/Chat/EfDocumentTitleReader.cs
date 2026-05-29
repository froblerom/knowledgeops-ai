using KnowledgeOps.Application.Chat.Citations;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class EfDocumentTitleReader(KnowledgeOpsDbContext db) : IDocumentTitleReader
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetTitlesAsync(
        IReadOnlyList<Guid> documentIds,
        Guid organizationId,
        CancellationToken ct = default)
    {
        if (documentIds.Count == 0)
            return new Dictionary<Guid, string>();

        var results = await db.Documents
            .AsNoTracking()
            .Where(doc =>
                documentIds.Contains(doc.Id)
                && doc.OrganizationId == organizationId
                && doc.DeletedAt == null)
            .Select(doc => new { doc.Id, doc.Title })
            .ToListAsync(ct);

        return results.ToDictionary(r => r.Id, r => r.Title);
    }
}

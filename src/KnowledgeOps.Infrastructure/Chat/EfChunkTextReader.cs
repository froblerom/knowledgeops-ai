using KnowledgeOps.Application.Chat;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class EfChunkTextReader(KnowledgeOpsDbContext db) : IChunkTextReader
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetChunkTextsAsync(
        IReadOnlyList<Guid> chunkIds,
        Guid organizationId,
        CancellationToken ct = default)
    {
        var dict = await db.DocumentChunks
            .Where(c => chunkIds.Contains(c.Id) && c.OrganizationId == organizationId)
            .Select(c => new { c.Id, c.Text })
            .ToDictionaryAsync(c => c.Id, c => c.Text, ct);

        return dict;
    }
}

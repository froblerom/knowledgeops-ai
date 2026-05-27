using KnowledgeOps.Application.Documents;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Documents;

internal sealed class EfDocumentChunkRepository(KnowledgeOpsDbContext dbContext) : IDocumentChunkRepository
{
    public async Task SaveChunksAsync(
        IReadOnlyList<DocumentChunkRecord> chunks,
        CancellationToken cancellationToken = default)
    {
        var entities = chunks.Select(c => new DocumentChunk
        {
            Id = c.ChunkId,
            DocumentId = c.DocumentId,
            OrganizationId = c.OrganizationId,
            ChunkIndex = c.ChunkIndex,
            Text = c.Text,
            CharacterLength = c.CharacterLength,
            TokenEstimate = c.TokenEstimate,
            CreatedAt = c.CreatedAt
        });

        dbContext.DocumentChunks.AddRange(entities);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunkRecord>> GetChunksForDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DocumentChunks
            .Where(c => c.DocumentId == documentId && c.DeletedAt == null)
            .OrderBy(c => c.ChunkIndex)
            .Select(c => new DocumentChunkRecord(
                c.Id,
                c.DocumentId,
                c.OrganizationId,
                c.ChunkIndex,
                c.Text,
                c.CharacterLength ?? 0,
                c.TokenEstimate ?? 0,
                c.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

using KnowledgeOps.Application.Documents;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Infrastructure.Persistence;

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
}

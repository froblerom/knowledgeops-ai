namespace KnowledgeOps.Application.Documents;

public sealed record DocumentChunkRecord(
    Guid ChunkId,
    Guid DocumentId,
    Guid OrganizationId,
    int ChunkIndex,
    string Text,
    int CharacterLength,
    int TokenEstimate,
    DateTimeOffset CreatedAt);

public interface IDocumentChunkRepository
{
    Task SaveChunksAsync(
        IReadOnlyList<DocumentChunkRecord> chunks,
        CancellationToken cancellationToken = default);
}

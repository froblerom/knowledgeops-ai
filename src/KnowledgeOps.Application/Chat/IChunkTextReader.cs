namespace KnowledgeOps.Application.Chat;

public interface IChunkTextReader
{
    Task<IReadOnlyDictionary<Guid, string>> GetChunkTextsAsync(
        IReadOnlyList<Guid> chunkIds,
        Guid organizationId,
        CancellationToken ct = default);
}

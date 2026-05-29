namespace KnowledgeOps.Application.Chat.Citations;

public interface IDocumentTitleReader
{
    Task<IReadOnlyDictionary<Guid, string>> GetTitlesAsync(
        IReadOnlyList<Guid> documentIds,
        Guid organizationId,
        CancellationToken ct = default);
}

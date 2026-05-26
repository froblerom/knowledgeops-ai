namespace KnowledgeOps.Application.Documents;

public interface IDocumentStorage
{
    Task<StoredDocumentReference> StoreAsync(
        Stream fileStream,
        string safeFileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageReference, CancellationToken cancellationToken = default);
}

public sealed record StoredDocumentReference(string Location);

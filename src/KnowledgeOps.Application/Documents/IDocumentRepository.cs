namespace KnowledgeOps.Application.Documents;

public interface IDocumentRepository
{
    Task<IReadOnlyList<ManagedDocument>> ListAsync(Guid organizationId, CancellationToken ct = default);
    Task<ManagedDocument?> FindAsync(Guid documentId, Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<ManagedDocument>> FindFailedDocumentsAsync(Guid organizationId, int limit, CancellationToken ct = default);
    Task<ManagedDocument> CreateAsync(NewManagedDocument document, CancellationToken ct = default);
    Task<DocumentDisableResult?> DisableRetrievalAsync(Guid documentId, Guid organizationId, DateTimeOffset updatedAt, CancellationToken ct = default);
    Task<DocumentEnableResult?> EnableRetrievalAsync(Guid documentId, Guid organizationId, DateTimeOffset updatedAt, CancellationToken ct = default);

    // Processing lifecycle — used by the Worker only, not by the upload request path.
    Task<IReadOnlyList<ManagedDocument>> FindPendingForProcessingAsync(int maxCount, CancellationToken ct = default);
    Task<ManagedDocument?> ClaimForProcessingAsync(Guid documentId, DateTimeOffset now, CancellationToken ct = default);
    Task<ManagedDocument?> MarkProcessedAsync(Guid documentId, DateTimeOffset now, CancellationToken ct = default);
    Task<ManagedDocument?> MarkFailedAsync(Guid documentId, string safeFailureReason, DateTimeOffset now, CancellationToken ct = default);
}

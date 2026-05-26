namespace KnowledgeOps.Application.Documents;

public interface IDocumentRepository
{
    Task<IReadOnlyList<ManagedDocument>> ListAsync(Guid organizationId, CancellationToken ct = default);
    Task<ManagedDocument?> FindAsync(Guid documentId, Guid organizationId, CancellationToken ct = default);
    Task<ManagedDocument> CreateAsync(NewManagedDocument document, CancellationToken ct = default);
    Task<DocumentDisableResult?> DisableRetrievalAsync(Guid documentId, Guid organizationId, DateTimeOffset updatedAt, CancellationToken ct = default);
}

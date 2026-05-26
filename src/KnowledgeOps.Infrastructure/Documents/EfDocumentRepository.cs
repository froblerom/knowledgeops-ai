using KnowledgeOps.Application.Documents;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Documents;

internal sealed class EfDocumentRepository(KnowledgeOpsDbContext dbContext) : IDocumentRepository
{
    public async Task<IReadOnlyList<ManagedDocument>> ListAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var documents = await dbContext.Documents
            .AsNoTracking()
            .Where(doc => doc.OrganizationId == organizationId && doc.DeletedAt == null)
            .OrderByDescending(doc => doc.UploadedAt)
            .ThenBy(doc => doc.Title)
            .ToListAsync(ct);

        return documents.Select(Map).ToArray();
    }

    public async Task<ManagedDocument?> FindAsync(
        Guid documentId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        var document = await dbContext.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                doc => doc.Id == documentId
                    && doc.OrganizationId == organizationId
                    && doc.DeletedAt == null,
                ct);

        return document is null ? null : Map(document);
    }

    public async Task<ManagedDocument> CreateAsync(NewManagedDocument newDocument, CancellationToken ct = default)
    {
        // ProcessingStatus defaults to DocumentProcessingStatus.Uploaded (enum 0, first member).
        // IsRetrievalEnabled defaults to false (bool default) and the column has DEFAULT false.
        // Both match the required initial state for newly uploaded documents.
        var document = new Document
        {
            Id = newDocument.DocumentId,
            OrganizationId = newDocument.OrganizationId,
            FileName = newDocument.FileName,
            Title = newDocument.Title,
            ContentType = newDocument.ContentType,
            FileSizeBytes = newDocument.FileSizeBytes,
            StorageLocation = newDocument.StorageLocation,
            UploadedByUserId = newDocument.UploadedByUserId,
            UploadedAt = newDocument.UploadedAt,
            CreatedAt = newDocument.CreatedAt,
            UpdatedAt = newDocument.CreatedAt
        };

        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(ct);
        return Map(document);
    }

    public async Task<DocumentDisableResult?> DisableRetrievalAsync(
        Guid documentId,
        Guid organizationId,
        DateTimeOffset updatedAt,
        CancellationToken ct = default)
    {
        var changedRows = await dbContext.Documents
            .Where(
                doc => doc.Id == documentId
                    && doc.OrganizationId == organizationId
                    && doc.DeletedAt == null
                    && doc.IsRetrievalEnabled)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(doc => doc.IsRetrievalEnabled, false)
                    .SetProperty(doc => doc.UpdatedAt, updatedAt),
                ct);

        var document = await FindAsync(documentId, organizationId, ct);
        return document is null
            ? null
            : new DocumentDisableResult(document, changedRows == 1);
    }

    private static ManagedDocument Map(Document doc) =>
        new(
            doc.Id,
            doc.OrganizationId,
            doc.FileName,
            doc.Title,
            doc.ContentType,
            doc.FileSizeBytes,
            doc.ProcessingStatus,
            doc.FailureReason,
            doc.IsRetrievalEnabled,
            doc.UploadedByUserId,
            doc.UploadedAt,
            doc.ProcessingStartedAt,
            doc.ProcessedAt,
            doc.CreatedAt,
            doc.UpdatedAt,
            doc.DeletedAt);
}

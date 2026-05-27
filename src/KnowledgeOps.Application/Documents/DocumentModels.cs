using KnowledgeOps.Domain.Documents;

namespace KnowledgeOps.Application.Documents;

public sealed record DocumentActor(Guid UserId, Guid OrganizationId);

public sealed record ManagedDocument(
    Guid DocumentId,
    Guid OrganizationId,
    string FileName,
    string Title,
    string ContentType,
    long FileSizeBytes,
    string StorageLocation,
    DocumentProcessingStatus ProcessingStatus,
    string? FailureReason,
    bool IsRetrievalEnabled,
    Guid UploadedByUserId,
    DateTimeOffset UploadedAt,
    DateTimeOffset? ProcessingStartedAt,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt);

public sealed record NewManagedDocument(
    Guid DocumentId,
    Guid OrganizationId,
    string FileName,
    string Title,
    string ContentType,
    long FileSizeBytes,
    string StorageLocation,
    Guid UploadedByUserId,
    DateTimeOffset UploadedAt,
    DateTimeOffset CreatedAt);

public sealed record DocumentDisableResult(ManagedDocument Document, bool WasChanged);

public sealed record UploadDocumentCommand(
    string Title,
    Stream FileStream,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes);

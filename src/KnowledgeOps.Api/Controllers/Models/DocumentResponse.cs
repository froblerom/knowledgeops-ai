namespace KnowledgeOps.Api.Controllers.Models;

public sealed class DocumentResponse
{
    public Guid DocumentId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string ProcessingStatus { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
    public bool IsRetrievalEnabled { get; init; }
    public Guid UploadedByUserId { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public DateTimeOffset? ProcessingStartedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }
}

public sealed class DocumentProcessingStatusResponse
{
    public Guid DocumentId { get; init; }
    public string ProcessingStatus { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
    public bool IsRetrievalEnabled { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public DateTimeOffset? ProcessingStartedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }
}

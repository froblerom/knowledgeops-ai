namespace KnowledgeOps.Domain.Documents;

public sealed class Document
{
    public const int FailureReasonMaxLength = 1000;

    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string StorageLocation { get; init; } = string.Empty;
    public DocumentProcessingStatus ProcessingStatus { get; private set; }
    public string? FailureReason { get; private set; }
    public bool IsRetrievalEnabled { get; private set; }
    public Guid UploadedByUserId { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public DateTimeOffset? ProcessingStartedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public void StartProcessing(DateTimeOffset now)
    {
        if (ProcessingStatus != DocumentProcessingStatus.Uploaded)
            throw new InvalidOperationException("Only Uploaded documents can begin processing.");

        ProcessingStatus = DocumentProcessingStatus.Processing;
        ProcessingStartedAt = now;
        UpdatedAt = now;
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        if (ProcessingStatus != DocumentProcessingStatus.Processing)
            throw new InvalidOperationException("Only Processing documents can be marked Processed.");

        ProcessingStatus = DocumentProcessingStatus.Processed;
        ProcessedAt = now;
        UpdatedAt = now;
    }

    public void MarkFailed(string failureReason, DateTimeOffset now)
    {
        if (ProcessingStatus != DocumentProcessingStatus.Processing)
            throw new InvalidOperationException("Only Processing documents can be marked Failed.");

        var safeReason = failureReason?.Trim();
        if (string.IsNullOrWhiteSpace(safeReason))
            throw new ArgumentException("A failure reason is required.", nameof(failureReason));
        if (safeReason.Length > FailureReasonMaxLength)
            throw new ArgumentException(
                $"Failure reason cannot exceed {FailureReasonMaxLength} characters.",
                nameof(failureReason));

        ProcessingStatus = DocumentProcessingStatus.Failed;
        FailureReason = safeReason;
        UpdatedAt = now;
    }

    public bool DisableRetrieval()
    {
        if (!IsRetrievalEnabled)
            return false;
        IsRetrievalEnabled = false;
        return true;
    }

    public bool IsEligibleForRetrieval() =>
        ProcessingStatus == DocumentProcessingStatus.Processed
        && IsRetrievalEnabled
        && DeletedAt is null;
}

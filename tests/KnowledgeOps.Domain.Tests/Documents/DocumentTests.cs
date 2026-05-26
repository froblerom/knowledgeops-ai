using KnowledgeOps.Domain.Documents;

namespace KnowledgeOps.Domain.Tests.Documents;

public sealed class DocumentTests
{
    private static readonly DateTimeOffset UploadedAt = new(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ProcessingStatus_ContainsOnlyCanonicalValues()
    {
        Assert.Equal(
            ["Uploaded", "Processing", "Processed", "Failed"],
            Enum.GetNames<DocumentProcessingStatus>());
        Assert.DoesNotContain("Disabled", Enum.GetNames<DocumentProcessingStatus>());
    }

    [Fact]
    public void NewDocument_CapturesCanonicalMetadataAndStartsDisabled()
    {
        var doc = MakeDocument();

        Assert.Equal("policy.pdf", doc.FileName);
        Assert.Equal("Test Document", doc.Title);
        Assert.Equal("application/pdf", doc.ContentType);
        Assert.Equal(42L, doc.FileSizeBytes);
        Assert.Equal("pending://document-metadata-only", doc.StorageLocation);
        Assert.Equal(UploadedAt, doc.UploadedAt);
        Assert.Equal(UploadedAt, doc.CreatedAt);
        Assert.Equal(UploadedAt, doc.UpdatedAt);
        Assert.Equal(DocumentProcessingStatus.Uploaded, doc.ProcessingStatus);
        Assert.False(doc.IsRetrievalEnabled);
    }

    [Fact]
    public void StartProcessing_SetsStatusTimestampAndUpdatedAt()
    {
        var doc = MakeDocument();
        var now = UploadedAt.AddMinutes(1);

        doc.StartProcessing(now);

        Assert.Equal(DocumentProcessingStatus.Processing, doc.ProcessingStatus);
        Assert.Equal(now, doc.ProcessingStartedAt);
        Assert.Equal(now, doc.UpdatedAt);
    }

    [Fact]
    public void StartProcessing_WhenNotUploaded_Throws()
    {
        var doc = MakeDocument();
        doc.StartProcessing(UploadedAt.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => doc.StartProcessing(UploadedAt.AddMinutes(2)));
    }

    [Fact]
    public void MarkProcessed_SetsStatusTimestampAndUpdatedAt()
    {
        var doc = MakeDocument();
        doc.StartProcessing(UploadedAt.AddMinutes(1));
        var now = UploadedAt.AddMinutes(2);

        doc.MarkProcessed(now);

        Assert.Equal(DocumentProcessingStatus.Processed, doc.ProcessingStatus);
        Assert.Equal(now, doc.ProcessedAt);
        Assert.Equal(now, doc.UpdatedAt);
    }

    [Fact]
    public void MarkProcessed_WhenNotProcessing_Throws()
    {
        var doc = MakeDocument();

        Assert.Throws<InvalidOperationException>(() => doc.MarkProcessed(UploadedAt.AddMinutes(2)));
    }

    [Fact]
    public void MarkFailed_StoresSafeReasonTimestampAndStatus()
    {
        var doc = MakeDocument();
        doc.StartProcessing(UploadedAt.AddMinutes(1));
        var now = UploadedAt.AddMinutes(2);

        doc.MarkFailed("  Unsupported document encoding.  ", now);

        Assert.Equal(DocumentProcessingStatus.Failed, doc.ProcessingStatus);
        Assert.Equal("Unsupported document encoding.", doc.FailureReason);
        Assert.Equal(now, doc.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkFailed_WhenReasonBlank_Throws(string reason)
    {
        var doc = MakeDocument();
        doc.StartProcessing(UploadedAt.AddMinutes(1));

        Assert.Throws<ArgumentException>(() => doc.MarkFailed(reason, UploadedAt.AddMinutes(2)));
    }

    [Fact]
    public void MarkFailed_WhenReasonExceedsPersistenceLimit_Throws()
    {
        var doc = MakeDocument();
        doc.StartProcessing(UploadedAt.AddMinutes(1));

        Assert.Throws<ArgumentException>(() =>
            doc.MarkFailed(new string('x', Document.FailureReasonMaxLength + 1), UploadedAt.AddMinutes(2)));
    }

    [Fact]
    public void DisableRetrieval_WhenEnabled_IsIdempotentAndDoesNotChangeProcessingStatus()
    {
        var doc = MakeDocumentWithRetrievalEnabled();
        doc.StartProcessing(UploadedAt.AddMinutes(1));
        doc.MarkProcessed(UploadedAt.AddMinutes(2));

        Assert.True(doc.DisableRetrieval());
        Assert.False(doc.DisableRetrieval());
        Assert.False(doc.IsRetrievalEnabled);
        Assert.Equal(DocumentProcessingStatus.Processed, doc.ProcessingStatus);
    }

    [Fact]
    public void IsEligibleForRetrieval_RequiresProcessedEnabledAndNotSoftDeleted()
    {
        var uploaded = MakeDocumentWithRetrievalEnabled();
        Assert.False(uploaded.IsEligibleForRetrieval());

        uploaded.StartProcessing(UploadedAt.AddMinutes(1));
        Assert.False(uploaded.IsEligibleForRetrieval());

        uploaded.MarkProcessed(UploadedAt.AddMinutes(2));
        Assert.True(uploaded.IsEligibleForRetrieval());

        uploaded.DisableRetrieval();
        Assert.False(uploaded.IsEligibleForRetrieval());

        var failed = MakeDocumentWithRetrievalEnabled();
        failed.StartProcessing(UploadedAt.AddMinutes(1));
        failed.MarkFailed("Safe failure.", UploadedAt.AddMinutes(2));
        Assert.False(failed.IsEligibleForRetrieval());

        var deleted = MakeDocumentWithRetrievalEnabled();
        deleted.StartProcessing(UploadedAt.AddMinutes(1));
        deleted.MarkProcessed(UploadedAt.AddMinutes(2));
        deleted.DeletedAt = UploadedAt.AddMinutes(3);
        Assert.False(deleted.IsEligibleForRetrieval());
    }

    private static Document MakeDocument() =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            FileName = "policy.pdf",
            Title = "Test Document",
            ContentType = "application/pdf",
            FileSizeBytes = 42,
            StorageLocation = "pending://document-metadata-only",
            UploadedByUserId = Guid.NewGuid(),
            UploadedAt = UploadedAt,
            CreatedAt = UploadedAt,
            UpdatedAt = UploadedAt
        };

    private static Document MakeDocumentWithRetrievalEnabled()
    {
        var doc = MakeDocument();
        typeof(Document).GetProperty(nameof(Document.IsRetrievalEnabled))!.SetValue(doc, true);
        return doc;
    }
}

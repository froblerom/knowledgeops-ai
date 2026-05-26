using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Documents;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Documents;

public sealed class DocumentUploadServiceTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DocumentActor Actor = new(ActorId, OrgId);

    // ──────────────────────────────────────────────────────────────
    // Validation — rejected cases
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_EmptyTitle_ThrowsValidationException()
    {
        var service = BuildService();
        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            service.UploadAsync(Actor, ValidCommand(title: "  ")));
    }

    [Fact]
    public async Task UploadAsync_MissingTitle_ThrowsValidationException()
    {
        var service = BuildService();
        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            service.UploadAsync(Actor, ValidCommand(title: "")));
    }

    [Fact]
    public async Task UploadAsync_ZeroSizeFile_ThrowsValidationException()
    {
        var service = BuildService();
        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            service.UploadAsync(Actor, ValidCommand(fileSizeBytes: 0)));
    }

    [Fact]
    public async Task UploadAsync_OversizedFile_ThrowsValidationException()
    {
        var service = BuildService();
        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            service.UploadAsync(Actor, ValidCommand(fileSizeBytes: 10 * 1024 * 1024 + 1)));
    }

    [Theory]
    [InlineData("report.exe")]
    [InlineData("report.jpg")]
    [InlineData("report.zip")]
    [InlineData("report")]
    public async Task UploadAsync_UnsupportedExtension_ThrowsValidationException(string fileName)
    {
        var service = BuildService();
        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            service.UploadAsync(Actor, ValidCommand(fileName: fileName)));
    }

    [Fact]
    public async Task UploadAsync_UnsupportedContentType_ThrowsValidationException()
    {
        var service = BuildService();
        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            service.UploadAsync(Actor, ValidCommand(contentType: "image/png")));
    }

    [Fact]
    public async Task UploadAsync_EmptyFileName_ThrowsValidationException()
    {
        var service = BuildService();
        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            service.UploadAsync(Actor, ValidCommand(fileName: "")));
    }

    [Fact]
    public async Task UploadAsync_EmptyContentType_ThrowsValidationException()
    {
        var service = BuildService();
        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            service.UploadAsync(Actor, ValidCommand(contentType: "")));
    }

    [Fact]
    public async Task UploadAsync_ValidationFails_EmitsDocumentUploadRejectedAudit()
    {
        var audit = new RecordingAuditWriter();
        var storage = new FakeStorage();
        var repo = new TrackingRepository();
        var service = BuildService(repository: repo, storage: storage, audit: audit);

        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            service.UploadAsync(Actor, ValidCommand(title: "  ")));

        var rejected = Assert.Single(audit.Events, e => e.EventType == AuditEventTypes.DocumentUploadRejected);
        Assert.Equal(OrgId, rejected.OrganizationId);
        Assert.Equal(ActorId, rejected.UserId);
        Assert.DoesNotContain("byte", rejected.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repo.LastCreated);
        Assert.False(storage.WasDeleteCalled);
    }

    // ──────────────────────────────────────────────────────────────
    // Validation — accepted cases
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("policy.PDF")]
    [InlineData("policy.Pdf")]
    public async Task UploadAsync_ExtensionCaseInsensitive_Accepted(string fileName)
    {
        var repo = new TrackingRepository();
        var service = BuildService(repository: repo);
        await service.UploadAsync(Actor, ValidCommand(fileName: fileName));
        Assert.NotNull(repo.LastCreated);
    }

    [Theory]
    [InlineData("notes.md", "text/markdown")]
    [InlineData("notes.markdown", "text/markdown")]
    [InlineData("notes.txt", "text/plain")]
    [InlineData("doc.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public async Task UploadAsync_SupportedFormats_Accepted(string fileName, string contentType)
    {
        var repo = new TrackingRepository();
        var service = BuildService(repository: repo);
        await service.UploadAsync(Actor, ValidCommand(fileName: fileName, contentType: contentType));
        Assert.NotNull(repo.LastCreated);
    }

    [Fact]
    public async Task UploadAsync_ContentTypeWithParameters_Accepted()
    {
        var repo = new TrackingRepository();
        var service = BuildService(repository: repo);
        await service.UploadAsync(Actor, ValidCommand(contentType: "application/pdf; charset=utf-8"));
        Assert.NotNull(repo.LastCreated);
    }

    // ──────────────────────────────────────────────────────────────
    // Success path
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_Success_ReturnsUploadedStatus()
    {
        var repo = new TrackingRepository();
        var service = BuildService(repository: repo);

        var result = await service.UploadAsync(Actor, ValidCommand());

        Assert.Equal(DocumentProcessingStatus.Uploaded, result.ProcessingStatus);
    }

    [Fact]
    public async Task UploadAsync_Success_ReturnsIsRetrievalEnabledFalse()
    {
        var repo = new TrackingRepository();
        var service = BuildService(repository: repo);

        var result = await service.UploadAsync(Actor, ValidCommand());

        Assert.False(result.IsRetrievalEnabled);
    }

    [Fact]
    public async Task UploadAsync_Success_AssignsActorOrganization()
    {
        var repo = new TrackingRepository();
        var service = BuildService(repository: repo);

        await service.UploadAsync(Actor, ValidCommand());

        Assert.Equal(OrgId, repo.LastCreated!.OrganizationId);
    }

    [Fact]
    public async Task UploadAsync_Success_AssignsActorUserId()
    {
        var repo = new TrackingRepository();
        var service = BuildService(repository: repo);

        await service.UploadAsync(Actor, ValidCommand());

        Assert.Equal(ActorId, repo.LastCreated!.UploadedByUserId);
    }

    [Fact]
    public async Task UploadAsync_Success_StorageLocationSetFromStorageRef()
    {
        var repo = new TrackingRepository();
        var storage = new FakeStorage();
        var service = BuildService(repository: repo, storage: storage);

        await service.UploadAsync(Actor, ValidCommand());

        Assert.NotNull(repo.LastCreated!.StorageLocation);
        Assert.StartsWith("local://", repo.LastCreated.StorageLocation);
    }

    [Fact]
    public async Task UploadAsync_Success_EmitsDocumentUploadAcceptedAudit()
    {
        var audit = new RecordingAuditWriter();
        var service = BuildService(audit: audit);

        await service.UploadAsync(Actor, ValidCommand());

        Assert.Single(audit.Events, e => e.EventType == AuditEventTypes.DocumentUploadAccepted);
    }

    [Fact]
    public async Task UploadAsync_Success_AuditDoesNotContainFileBytes()
    {
        var audit = new RecordingAuditWriter();
        var service = BuildService(audit: audit);

        await service.UploadAsync(Actor, ValidCommand());

        var messages = string.Join("|", audit.Events.Select(e => e.Message));
        Assert.DoesNotContain("file-bytes", messages, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAsync_TrimmedTitle_TitleStoredTrimmed()
    {
        var repo = new TrackingRepository();
        var service = BuildService(repository: repo);

        await service.UploadAsync(Actor, ValidCommand(title: "  My Policy  "));

        Assert.Equal("My Policy", repo.LastCreated!.Title);
    }

    // ──────────────────────────────────────────────────────────────
    // Atomicity — storage failure
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_StorageFails_DoesNotCallCreateAsync()
    {
        var repo = new TrackingRepository();
        var storage = new FakeStorage { ShouldFail = true };
        var service = BuildService(repository: repo, storage: storage);

        await Assert.ThrowsAsync<ApplicationServiceUnavailableException>(() =>
            service.UploadAsync(Actor, ValidCommand()));

        Assert.Null(repo.LastCreated);
    }

    [Fact]
    public async Task UploadAsync_StorageFails_EmitsDocumentUploadFailedAudit()
    {
        var audit = new RecordingAuditWriter();
        var storage = new FakeStorage { ShouldFail = true };
        var service = BuildService(storage: storage, audit: audit);

        await Assert.ThrowsAsync<ApplicationServiceUnavailableException>(() =>
            service.UploadAsync(Actor, ValidCommand()));

        Assert.Single(audit.Events, e => e.EventType == AuditEventTypes.DocumentUploadFailed);
    }

    // ──────────────────────────────────────────────────────────────
    // Atomicity — metadata persistence failure
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_PersistenceFails_CallsDeleteAsyncOnStorage()
    {
        var repo = new TrackingRepository { ShouldFailCreate = true };
        var storage = new FakeStorage();
        var service = BuildService(repository: repo, storage: storage);

        await Assert.ThrowsAsync<ApplicationServiceUnavailableException>(() =>
            service.UploadAsync(Actor, ValidCommand()));

        Assert.True(storage.WasDeleteCalled);
    }

    [Fact]
    public async Task UploadAsync_PersistenceFails_EmitsDocumentUploadFailedAudit()
    {
        var repo = new TrackingRepository { ShouldFailCreate = true };
        var audit = new RecordingAuditWriter();
        var service = BuildService(repository: repo, audit: audit);

        await Assert.ThrowsAsync<ApplicationServiceUnavailableException>(() =>
            service.UploadAsync(Actor, ValidCommand()));

        Assert.Single(audit.Events, e => e.EventType == AuditEventTypes.DocumentUploadFailed);
    }

    // ──────────────────────────────────────────────────────────────
    // Scope drift — upload must not create RAG artifacts
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_Success_IsEligibleForRetrievalReturnsFalse()
    {
        var service = BuildService();
        var result = await service.UploadAsync(Actor, ValidCommand());
        // Result is a read DTO; verify via status and retrieval flag (not the domain entity).
        Assert.Equal(DocumentProcessingStatus.Uploaded, result.ProcessingStatus);
        Assert.False(result.IsRetrievalEnabled);
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static UploadDocumentCommand ValidCommand(
        string title = "Policy Document",
        string fileName = "policy.pdf",
        string contentType = "application/pdf",
        long fileSizeBytes = 1024) =>
        new(title, Stream.Null, fileName, contentType, fileSizeBytes);

    private static DocumentService BuildService(
        TrackingRepository? repository = null,
        FakeStorage? storage = null,
        RecordingAuditWriter? audit = null) =>
        new(
            repository ?? new TrackingRepository(),
            storage ?? new FakeStorage(),
            audit ?? new RecordingAuditWriter(),
            new StubCorrelationContext(),
            NullLogger<DocumentService>.Instance);

    private sealed class TrackingRepository : IDocumentRepository
    {
        public NewManagedDocument? LastCreated { get; private set; }
        public bool ShouldFailCreate { get; init; }

        public Task<IReadOnlyList<ManagedDocument>> ListAsync(Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ManagedDocument>>([]);

        public Task<ManagedDocument?> FindAsync(Guid documentId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);

        public Task<ManagedDocument> CreateAsync(NewManagedDocument document, CancellationToken ct = default)
        {
            if (ShouldFailCreate)
                throw new InvalidOperationException("Simulated persistence failure.");
            LastCreated = document;
            return Task.FromResult(new ManagedDocument(
                document.DocumentId,
                document.OrganizationId,
                document.FileName,
                document.Title,
                document.ContentType,
                document.FileSizeBytes,
                DocumentProcessingStatus.Uploaded,
                null,
                false,
                document.UploadedByUserId,
                document.UploadedAt,
                null,
                null,
                document.CreatedAt,
                document.CreatedAt,
                null));
        }

        public Task<DocumentDisableResult?> DisableRetrievalAsync(
            Guid documentId, Guid organizationId, DateTimeOffset updatedAt, CancellationToken ct = default) =>
            Task.FromResult<DocumentDisableResult?>(null);

        public Task<IReadOnlyList<ManagedDocument>> FindPendingForProcessingAsync(int maxCount, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ManagedDocument>>([]);

        public Task<ManagedDocument?> ClaimForProcessingAsync(Guid documentId, DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);

        public Task<ManagedDocument?> MarkProcessedAsync(Guid documentId, DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);

        public Task<ManagedDocument?> MarkFailedAsync(Guid documentId, string safeFailureReason, DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);
    }

    private sealed class FakeStorage : IDocumentStorage
    {
        public bool ShouldFail { get; init; }
        public bool WasDeleteCalled { get; private set; }

        public Task<StoredDocumentReference> StoreAsync(
            Stream fileStream, string safeFileName, string contentType, CancellationToken ct = default)
        {
            if (ShouldFail)
                throw new InvalidOperationException("Simulated storage failure.");
            return Task.FromResult(new StoredDocumentReference($"local://test-{safeFileName}"));
        }

        public Task DeleteAsync(string storageReference, CancellationToken ct = default)
        {
            WasDeleteCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAuditWriter : IAuditEventWriter
    {
        public List<AuditEvent> Events { get; } = [];
        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class StubCorrelationContext : ICorrelationContext
    {
        public string CorrelationId => "upload-test-correlation";
    }
}

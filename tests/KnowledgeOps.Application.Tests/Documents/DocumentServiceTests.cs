using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Documents;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Documents;

public sealed class DocumentServiceTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid OtherOrgId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid DocId = Guid.NewGuid();
    private static readonly DocumentActor Actor = new(ActorId, OrgId);

    [Fact]
    public async Task ListAsync_ReturnsOnlyActorOrganizationScope()
    {
        var repository = new FakeRepository();
        repository.AddDocument(MakeDocument(DocId, OrgId));
        repository.AddDocument(MakeDocument(Guid.NewGuid(), OtherOrgId));
        var service = BuildService(repository);

        var results = await service.ListAsync(Actor);

        Assert.Single(results);
        Assert.Equal(OrgId, repository.LastListOrganizationId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAsync_AbsentOrCrossOrganizationIsSafelyNotFound(bool crossOrganization)
    {
        var repository = new FakeRepository();
        if (crossOrganization)
            repository.AddDocument(MakeDocument(DocId, OtherOrgId));

        await Assert.ThrowsAsync<ApplicationNotFoundException>(() =>
            BuildService(repository).GetAsync(Actor, DocId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetProcessingStatusAsync_AbsentOrCrossOrganizationIsSafelyNotFound(bool crossOrganization)
    {
        var repository = new FakeRepository();
        if (crossOrganization)
            repository.AddDocument(MakeDocument(DocId, OtherOrgId));

        await Assert.ThrowsAsync<ApplicationNotFoundException>(() =>
            BuildService(repository).GetProcessingStatusAsync(Actor, DocId));
    }

    [Fact]
    public async Task DisableRetrievalAsync_RealTransition_DisablesPreservesStatusAndAuditsOnce()
    {
        var repository = new FakeRepository();
        var audit = new RecordingAuditWriter();
        repository.AddDocument(MakeDocument(
            DocId,
            OrgId,
            DocumentProcessingStatus.Processed,
            isRetrievalEnabled: true));
        var service = BuildService(repository, audit);

        var first = await service.DisableRetrievalAsync(Actor, DocId);
        var second = await service.DisableRetrievalAsync(Actor, DocId);

        Assert.False(first.IsRetrievalEnabled);
        Assert.False(second.IsRetrievalEnabled);
        Assert.Equal(DocumentProcessingStatus.Processed, second.ProcessingStatus);
        Assert.Single(audit.Events, e => e.EventType == AuditEventTypes.DocumentRetrievalDisabled);
    }

    [Fact]
    public async Task DisableRetrievalAsync_WhenRepositoryReportsNoTransition_EmitsNoAudit()
    {
        var repository = new FakeRepository { ForceNoChange = true };
        var audit = new RecordingAuditWriter();
        repository.AddDocument(MakeDocument(DocId, OrgId, isRetrievalEnabled: true));

        await BuildService(repository, audit).DisableRetrievalAsync(Actor, DocId);

        Assert.Empty(audit.Events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisableRetrievalAsync_AbsentOrCrossOrganizationIsSafelyNotFound(bool crossOrganization)
    {
        var repository = new FakeRepository();
        if (crossOrganization)
            repository.AddDocument(MakeDocument(DocId, OtherOrgId, isRetrievalEnabled: true));

        await Assert.ThrowsAsync<ApplicationNotFoundException>(() =>
            BuildService(repository).DisableRetrievalAsync(Actor, DocId));
    }

    [Fact]
    public async Task DisableRetrievalAsync_AuditContainsNoMetadataOrStorageLocation()
    {
        var repository = new FakeRepository();
        var audit = new RecordingAuditWriter();
        repository.AddDocument(MakeDocument(DocId, OrgId, isRetrievalEnabled: true, title: "Sensitive Title"));

        await BuildService(repository, audit).DisableRetrievalAsync(Actor, DocId);

        var messages = string.Join("|", audit.Events.Select(e => e.Message));
        Assert.DoesNotContain("Sensitive Title", messages, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pending://", messages, StringComparison.OrdinalIgnoreCase);
    }

    private static DocumentService BuildService(IDocumentRepository repository, RecordingAuditWriter? audit = null) =>
        new(
            repository,
            audit ?? new RecordingAuditWriter(),
            new StubCorrelationContext(),
            NullLogger<DocumentService>.Instance);

    private static ManagedDocument MakeDocument(
        Guid documentId,
        Guid organizationId,
        DocumentProcessingStatus status = DocumentProcessingStatus.Uploaded,
        bool isRetrievalEnabled = false,
        string title = "Test Document") =>
        new(
            documentId,
            organizationId,
            "policy.pdf",
            title,
            "application/pdf",
            42,
            status,
            null,
            isRetrievalEnabled,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            null,
            status == DocumentProcessingStatus.Processed ? DateTimeOffset.UtcNow : null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null);

    private sealed class FakeRepository : IDocumentRepository
    {
        private readonly Dictionary<Guid, ManagedDocument> _documents = [];
        public Guid LastListOrganizationId { get; private set; }
        public bool ForceNoChange { get; init; }

        public void AddDocument(ManagedDocument doc) => _documents[doc.DocumentId] = doc;

        public Task<IReadOnlyList<ManagedDocument>> ListAsync(Guid organizationId, CancellationToken ct = default)
        {
            LastListOrganizationId = organizationId;
            return Task.FromResult<IReadOnlyList<ManagedDocument>>(
                _documents.Values.Where(d => d.OrganizationId == organizationId).ToArray());
        }

        public Task<ManagedDocument?> FindAsync(Guid documentId, Guid organizationId, CancellationToken ct = default)
        {
            _documents.TryGetValue(documentId, out var doc);
            return Task.FromResult(doc?.OrganizationId == organizationId ? doc : null);
        }

        public Task<ManagedDocument> CreateAsync(NewManagedDocument document, CancellationToken ct = default) =>
            Task.FromResult(new ManagedDocument(
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

        public Task<DocumentDisableResult?> DisableRetrievalAsync(
            Guid documentId,
            Guid organizationId,
            DateTimeOffset updatedAt,
            CancellationToken ct = default)
        {
            if (!_documents.TryGetValue(documentId, out var doc) || doc.OrganizationId != organizationId)
                return Task.FromResult<DocumentDisableResult?>(null);

            var changed = doc.IsRetrievalEnabled && !ForceNoChange;
            var updated = changed ? doc with { IsRetrievalEnabled = false, UpdatedAt = updatedAt } : doc;
            _documents[documentId] = updated;
            return Task.FromResult<DocumentDisableResult?>(new DocumentDisableResult(updated, changed));
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
        public string CorrelationId => "safe-correlation-id";
    }
}

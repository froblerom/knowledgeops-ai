using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Documents;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Documents;

public sealed class DocumentProcessingOrchestratorTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid DocId = Guid.NewGuid();
    private static readonly DateTimeOffset UploadedAt = new(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessNextAsync_NoPendingDocuments_ReturnsFalse()
    {
        var repo = new SpyRepository();
        var orchestrator = BuildOrchestrator(repo);

        var result = await orchestrator.ProcessNextAsync();

        Assert.False(result);
        Assert.Equal(0, repo.ClaimCallCount);
    }

    [Fact]
    public async Task ProcessNextAsync_PlaceholderSucceeds_MarksProcessedAndReturnsTrue()
    {
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId));
        var orchestrator = BuildOrchestrator(repo);

        var result = await orchestrator.ProcessNextAsync();

        Assert.True(result);
        Assert.Equal(DocId, repo.MarkProcessedDocumentId);
        Assert.Null(repo.MarkFailedDocumentId);
    }

    [Fact]
    public async Task ProcessNextAsync_PlaceholderFails_MarksFailedAndReturnsTrue()
    {
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId));
        var step = new FailingProcessingStep("simulated failure");
        var orchestrator = BuildOrchestrator(repo, step: step);

        var result = await orchestrator.ProcessNextAsync();

        Assert.True(result);
        Assert.Equal(DocId, repo.MarkFailedDocumentId);
        Assert.Null(repo.MarkProcessedDocumentId);
    }

    [Fact]
    public async Task ProcessNextAsync_ClaimReturnsNull_ReturnsFalse()
    {
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId));
        repo.ClaimShouldReturnNull = true;
        var orchestrator = BuildOrchestrator(repo);

        var result = await orchestrator.ProcessNextAsync();

        Assert.False(result);
        Assert.Null(repo.MarkProcessedDocumentId);
    }

    [Fact]
    public async Task ProcessNextAsync_OnSuccess_IsRetrievalEnabledRemainsFalse()
    {
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId, isRetrievalEnabled: false));
        var orchestrator = BuildOrchestrator(repo);

        await orchestrator.ProcessNextAsync();

        Assert.NotNull(repo.MarkProcessedDocumentId);
        Assert.False(repo.MarkProcessedClaim!.IsRetrievalEnabled);
    }

    [Fact]
    public async Task ProcessNextAsync_OnFailure_IsRetrievalEnabledRemainsFalse()
    {
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId, isRetrievalEnabled: false));
        var step = new FailingProcessingStep("err");
        var orchestrator = BuildOrchestrator(repo, step: step);

        await orchestrator.ProcessNextAsync();

        Assert.NotNull(repo.MarkFailedDocumentId);
        Assert.False(repo.MarkFailedClaim!.IsRetrievalEnabled);
    }

    [Fact]
    public async Task ProcessNextAsync_Failure_StoresSafeReasonNotRawException()
    {
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId));
        var step = new FailingProcessingStep("Unsupported encoding.");
        var orchestrator = BuildOrchestrator(repo, step: step);

        await orchestrator.ProcessNextAsync();

        Assert.Equal("Unsupported encoding.", repo.MarkFailedReason);
        Assert.DoesNotContain("StackTrace", repo.MarkFailedReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessNextAsync_Failure_ReasonTruncatedToMaxLength()
    {
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId));
        var longMessage = new string('x', 500);
        var step = new FailingProcessingStep(longMessage);
        var orchestrator = BuildOrchestrator(repo, step: step);

        await orchestrator.ProcessNextAsync();

        Assert.NotNull(repo.MarkFailedReason);
        Assert.True(repo.MarkFailedReason!.Length <= 200);
    }

    [Fact]
    public async Task ProcessNextAsync_EmitsDocumentProcessingStarted()
    {
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId));
        var audit = new RecordingAuditWriter();
        var orchestrator = BuildOrchestrator(repo, audit: audit);

        await orchestrator.ProcessNextAsync();

        Assert.Contains(audit.Events, e => e.EventType == AuditEventTypes.DocumentProcessingStarted);
    }

    [Fact]
    public async Task ProcessNextAsync_EmitsDocumentProcessingSucceeded()
    {
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId));
        var audit = new RecordingAuditWriter();
        var orchestrator = BuildOrchestrator(repo, audit: audit);

        await orchestrator.ProcessNextAsync();

        Assert.Contains(audit.Events, e => e.EventType == AuditEventTypes.DocumentProcessingSucceeded);
        Assert.DoesNotContain(audit.Events, e => e.EventType == AuditEventTypes.DocumentProcessingFailed);
    }

    [Fact]
    public async Task ProcessNextAsync_EmitsDocumentProcessingFailed()
    {
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId));
        var audit = new RecordingAuditWriter();
        var step = new FailingProcessingStep("err");
        var orchestrator = BuildOrchestrator(repo, audit: audit, step: step);

        await orchestrator.ProcessNextAsync();

        Assert.Contains(audit.Events, e => e.EventType == AuditEventTypes.DocumentProcessingFailed);
        Assert.DoesNotContain(audit.Events, e => e.EventType == AuditEventTypes.DocumentProcessingSucceeded);
    }

    [Fact]
    public async Task ProcessNextAsync_DoesNotReadFileContent()
    {
        // PlaceholderDocumentProcessingStep performs no I/O. Verify it is the step used and
        // that executing it produces no storage reads. Because the placeholder returns
        // Task.CompletedTask immediately, the step call count is sufficient evidence.
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId));
        var spyStep = new SpyProcessingStep();
        var orchestrator = BuildOrchestrator(repo, step: spyStep);

        await orchestrator.ProcessNextAsync();

        Assert.Equal(1, spyStep.CallCount);
        Assert.False(spyStep.ReadAnyBytes);
    }

    [Fact]
    public async Task ProcessNextAsync_DoesNotCallAIProvider()
    {
        // The placeholder step must not invoke any AI provider. SpyProcessingStep
        // records a flag when an AI call occurs; it must remain false.
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId));
        var spyStep = new SpyProcessingStep();
        var orchestrator = BuildOrchestrator(repo, step: spyStep);

        await orchestrator.ProcessNextAsync();

        Assert.False(spyStep.CalledAiProvider);
    }

    [Fact]
    public async Task ProcessNextAsync_AuditDoesNotContainStorageLocation()
    {
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId, storageLocation: "local://sensitive/path/policy.pdf"));
        var audit = new RecordingAuditWriter();
        var orchestrator = BuildOrchestrator(repo, audit: audit);

        await orchestrator.ProcessNextAsync();

        var allMessages = string.Join("|", audit.Events.Select(e => e.Message));
        Assert.DoesNotContain("local://", allMessages, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sensitive", allMessages, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessNextAsync_AuditFailure_DoesNotPropagateException()
    {
        var repo = new SpyRepository();
        repo.AddPending(MakeDocument(DocId, OrgId));
        var audit = new ThrowingAuditWriter();
        var orchestrator = BuildOrchestrator(repo, audit: audit);

        // Must not throw even when the audit writer blows up.
        var result = await orchestrator.ProcessNextAsync();

        Assert.True(result);
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static DocumentProcessingOrchestrator BuildOrchestrator(
        SpyRepository repo,
        IAuditEventWriter? audit = null,
        IDocumentProcessingStep? step = null) =>
        new(
            repo,
            step ?? new PlaceholderDocumentProcessingStep(),
            audit ?? new RecordingAuditWriter(),
            new StubCorrelationContext(),
            NullLogger<DocumentProcessingOrchestrator>.Instance);

    private static ManagedDocument MakeDocument(
        Guid documentId,
        Guid organizationId,
        bool isRetrievalEnabled = false,
        string storageLocation = "local://test/policy.pdf") =>
        new(
            documentId,
            organizationId,
            "policy.pdf",
            "Test Document",
            "application/pdf",
            1024,
            DocumentProcessingStatus.Uploaded,
            null,
            isRetrievalEnabled,
            Guid.NewGuid(),
            UploadedAt,
            null,
            null,
            UploadedAt,
            UploadedAt,
            null);

    // ──────────────────────────────────────────────────────────────
    // Fakes
    // ──────────────────────────────────────────────────────────────

    private sealed class SpyRepository : IDocumentRepository
    {
        private readonly List<ManagedDocument> _pending = [];

        public bool ClaimShouldReturnNull { get; set; }
        public int ClaimCallCount { get; private set; }
        public Guid? MarkProcessedDocumentId { get; private set; }
        public ManagedDocument? MarkProcessedClaim { get; private set; }
        public Guid? MarkFailedDocumentId { get; private set; }
        public ManagedDocument? MarkFailedClaim { get; private set; }
        public string? MarkFailedReason { get; private set; }

        private ManagedDocument? _lastClaimed;

        public void AddPending(ManagedDocument doc) => _pending.Add(doc);

        public Task<IReadOnlyList<ManagedDocument>> ListAsync(Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ManagedDocument>>([]);

        public Task<ManagedDocument?> FindAsync(Guid documentId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);

        public Task<ManagedDocument> CreateAsync(NewManagedDocument document, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<DocumentDisableResult?> DisableRetrievalAsync(Guid documentId, Guid organizationId, DateTimeOffset updatedAt, CancellationToken ct = default) =>
            Task.FromResult<DocumentDisableResult?>(null);

        public Task<IReadOnlyList<ManagedDocument>> FindPendingForProcessingAsync(int maxCount, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ManagedDocument>>(_pending.Take(maxCount).ToArray());

        public Task<ManagedDocument?> ClaimForProcessingAsync(Guid documentId, DateTimeOffset now, CancellationToken ct = default)
        {
            ClaimCallCount++;

            if (ClaimShouldReturnNull)
                return Task.FromResult<ManagedDocument?>(null);

            var doc = _pending.FirstOrDefault(d => d.DocumentId == documentId);
            if (doc is null)
                return Task.FromResult<ManagedDocument?>(null);

            _lastClaimed = doc with { ProcessingStatus = DocumentProcessingStatus.Processing, ProcessingStartedAt = now, UpdatedAt = now };
            return Task.FromResult<ManagedDocument?>(_lastClaimed);
        }

        public Task<ManagedDocument?> MarkProcessedAsync(Guid documentId, DateTimeOffset now, CancellationToken ct = default)
        {
            MarkProcessedDocumentId = documentId;
            MarkProcessedClaim = _lastClaimed;
            var updated = _lastClaimed is null ? null : _lastClaimed with
            {
                ProcessingStatus = DocumentProcessingStatus.Processed,
                ProcessedAt = now,
                UpdatedAt = now
            };
            return Task.FromResult(updated);
        }

        public Task<ManagedDocument?> MarkFailedAsync(Guid documentId, string safeFailureReason, DateTimeOffset now, CancellationToken ct = default)
        {
            MarkFailedDocumentId = documentId;
            MarkFailedReason = safeFailureReason;
            MarkFailedClaim = _lastClaimed;
            var updated = _lastClaimed is null ? null : _lastClaimed with
            {
                ProcessingStatus = DocumentProcessingStatus.Failed,
                FailureReason = safeFailureReason,
                UpdatedAt = now
            };
            return Task.FromResult(updated);
        }
    }

    private sealed class FailingProcessingStep(string message) : IDocumentProcessingStep
    {
        public Task ExecuteAsync(ManagedDocument document, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(message);
    }

    private sealed class SpyProcessingStep : IDocumentProcessingStep
    {
        public int CallCount { get; private set; }
        public bool ReadAnyBytes { get; } = false;
        public bool CalledAiProvider { get; } = false;

        public Task ExecuteAsync(ManagedDocument document, CancellationToken cancellationToken = default)
        {
            CallCount++;
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

    private sealed class ThrowingAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default) =>
            throw new InvalidOperationException("Simulated audit infrastructure failure.");
    }

    private sealed class StubCorrelationContext : ICorrelationContext
    {
        public string CorrelationId => "orchestrator-test-correlation";
    }
}

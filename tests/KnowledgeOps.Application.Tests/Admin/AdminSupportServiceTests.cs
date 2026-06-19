using KnowledgeOps.Application.Admin;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Documents;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Admin;

public sealed class AdminSupportServiceTests
{
    private static readonly Guid OrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherOrgId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DocumentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessingFailures_ReturnsFailedDocumentsForCurrentOrganization()
    {
        var docs = new FakeDocumentRepository();
        docs.Add(MakeDocument(DocumentId, OrgId, DocumentProcessingStatus.Failed, "TextExtractionFailed"));
        var service = CreateService(docs, roles: ["KnowledgeAdmin"]);

        var result = await service.GetProcessingFailuresAsync(null);

        var item = Assert.Single(result);
        Assert.Equal(DocumentId, item.DocumentId);
        Assert.Equal("Failed", item.ProcessingStatus);
        Assert.Equal("TextExtractionFailed", item.FailureReason);
        Assert.Equal(Now, item.FailedAt);
    }

    [Fact]
    public async Task ProcessingFailures_ExcludesOtherOrganizationDocuments()
    {
        var docs = new FakeDocumentRepository();
        docs.Add(MakeDocument(DocumentId, OrgId, DocumentProcessingStatus.Failed, "TextExtractionFailed"));
        docs.Add(MakeDocument(Guid.NewGuid(), OtherOrgId, DocumentProcessingStatus.Failed, "OtherOrgFailure"));
        var service = CreateService(docs, roles: ["KnowledgeAdmin"]);

        var result = await service.GetProcessingFailuresAsync(null);

        Assert.Single(result);
        Assert.Equal(OrgId, docs.LastFailedOrganizationId);
        Assert.DoesNotContain(result, item => item.FailureReason == "OtherOrgFailure");
    }

    [Fact]
    public async Task ProcessingFailures_DoesNotExposeStorageLocation()
    {
        var docs = new FakeDocumentRepository();
        docs.Add(MakeDocument(DocumentId, OrgId, DocumentProcessingStatus.Failed, "TextExtractionFailed"));
        var service = CreateService(docs, roles: ["Admin"]);

        var result = await service.GetProcessingFailuresAsync(null);
        var serialized = System.Text.Json.JsonSerializer.Serialize(result);

        Assert.DoesNotContain("storageLocation", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("local://", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Agent")]
    [InlineData("Supervisor")]
    [InlineData("Manager")]
    public async Task ProcessingFailures_DeniedRolesThrowForbidden(string role)
    {
        var service = CreateService(new FakeDocumentRepository(), roles: [role]);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(() =>
            service.GetProcessingFailuresAsync(null));
    }

    [Fact]
    public async Task AuditLog_ReturnsEntriesForCurrentOrganization()
    {
        var audit = new FakeAuditLogRepository();
        audit.Add(MakeAuditLog(OrgId, "DocumentUploadAccepted"));
        var service = CreateService(new FakeDocumentRepository(), audit, roles: ["Admin"]);

        var result = await service.GetAuditLogAsync(new AuditLogQuery(null, null, null));

        var item = Assert.Single(result);
        Assert.Equal("DocumentUploadAccepted", item.EventType);
        Assert.Equal(OrgId, audit.LastOrganizationId);
    }

    [Fact]
    public async Task AuditLog_ExcludesOtherOrganizationEntries()
    {
        var audit = new FakeAuditLogRepository();
        audit.Add(MakeAuditLog(OrgId, "DocumentUploadAccepted"));
        audit.Add(MakeAuditLog(OtherOrgId, "DocumentProcessingFailed", "Other org row"));
        var service = CreateService(new FakeDocumentRepository(), audit, roles: ["Admin"]);

        var result = await service.GetAuditLogAsync(new AuditLogQuery(null, null, null));

        Assert.Single(result);
        Assert.DoesNotContain(result, row => row.Message == "Other org row");
    }

    [Fact]
    public async Task AuditLog_FiltersByDateRange()
    {
        var audit = new FakeAuditLogRepository();
        audit.Add(MakeAuditLog(OrgId, "Early", createdAt: Now.AddDays(-5)));
        audit.Add(MakeAuditLog(OrgId, "Inside", createdAt: Now.AddHours(-1)));
        audit.Add(MakeAuditLog(OrgId, "Late", createdAt: Now.AddDays(1)));
        var service = CreateService(new FakeDocumentRepository(), audit, roles: ["Admin"]);

        var result = await service.GetAuditLogAsync(new AuditLogQuery(Now.AddDays(-1), Now, null));

        var item = Assert.Single(result);
        Assert.Equal("Inside", item.EventType);
    }

    [Fact]
    public async Task AuditLog_FiltersByEventType()
    {
        var audit = new FakeAuditLogRepository();
        audit.Add(MakeAuditLog(OrgId, "DocumentUploadAccepted"));
        audit.Add(MakeAuditLog(OrgId, "DocumentProcessingFailed"));
        var service = CreateService(new FakeDocumentRepository(), audit, roles: ["Admin"]);

        var result = await service.GetAuditLogAsync(new AuditLogQuery(null, null, "DocumentProcessingFailed"));

        var item = Assert.Single(result);
        Assert.Equal("DocumentProcessingFailed", item.EventType);
    }

    [Fact]
    public async Task AuditLog_EmitsAuditLogViewed()
    {
        var writer = new RecordingAuditWriter();
        var audit = new FakeAuditLogRepository();
        audit.Add(MakeAuditLog(OrgId, "DocumentUploadAccepted"));
        var service = CreateService(new FakeDocumentRepository(), audit, writer, roles: ["Admin"]);

        await service.GetAuditLogAsync(new AuditLogQuery(null, null, null));

        Assert.Contains(writer.Events, e => e.EventType == AuditEventTypes.AuditLogViewed);
    }

    [Fact]
    public async Task AuditLog_AuditLogViewedMessageIsSafe()
    {
        var writer = new RecordingAuditWriter();
        var audit = new FakeAuditLogRepository();
        audit.Add(MakeAuditLog(OrgId, "DocumentUploadAccepted", "Returned audit row message"));
        var service = CreateService(new FakeDocumentRepository(), audit, writer, roles: ["Admin"]);

        await service.GetAuditLogAsync(new AuditLogQuery(Now.AddDays(-1), Now, "DocumentUploadAccepted"));

        var evt = Assert.Single(writer.Events, e => e.EventType == AuditEventTypes.AuditLogViewed);
        Assert.Contains("Count=1", evt.Message, StringComparison.Ordinal);
        Assert.Contains("EventTypeFilterSet=True", evt.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Returned audit row message", evt.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("password", evt.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", evt.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection string", evt.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Agent")]
    [InlineData("Supervisor")]
    [InlineData("KnowledgeAdmin")]
    [InlineData("Manager")]
    public async Task AuditLog_DeniedRolesThrowForbidden(string role)
    {
        var service = CreateService(new FakeDocumentRepository(), roles: [role]);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(() =>
            service.GetAuditLogAsync(new AuditLogQuery(null, null, null)));
    }

    private static AdminSupportService CreateService(
        FakeDocumentRepository docs,
        FakeAuditLogRepository? audit = null,
        RecordingAuditWriter? writer = null,
        IReadOnlyList<string>? roles = null)
    {
        var state = new UserAccessState(UserId, OrgId, roles ?? ["Admin"]);
        return new AdminSupportService(
            new FakeCurrentUser(),
            new FakeAccessStateReader(state),
            new PermissionService(),
            docs,
            audit ?? new FakeAuditLogRepository(),
            writer ?? new RecordingAuditWriter(),
            new StubCorrelationContext(),
            NullLogger<AdminSupportService>.Instance);
    }

    private static ManagedDocument MakeDocument(
        Guid documentId,
        Guid organizationId,
        DocumentProcessingStatus status,
        string? failureReason) =>
        new(
            documentId,
            organizationId,
            "unsafe-file-name.pdf",
            "Example policy",
            "application/pdf",
            100,
            "local://must-not-leak",
            status,
            failureReason,
            false,
            UserId,
            Now.AddDays(-1),
            Now.AddHours(-2),
            null,
            Now.AddDays(-1),
            Now,
            null);

    private static StoredAuditLogEntry MakeAuditLog(
        Guid organizationId,
        string eventType,
        string message = "Safe audit message.",
        DateTimeOffset? createdAt = null) =>
        new(
            Guid.NewGuid(),
            organizationId,
            UserId,
            eventType,
            message,
            "Info",
            "Document",
            DocumentId,
            "safe-correlation",
            createdAt ?? Now);

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId => AdminSupportServiceTests.UserId;
        public Guid OrganizationId => OrgId;
        public string Email => "admin@example.test";
        public string DisplayName => "Admin";
        public IReadOnlyList<string> Roles => [];
    }

    private sealed class FakeAccessStateReader(UserAccessState? state) : IUserAccessStateReader
    {
        public Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(state);
    }

    private sealed class StubCorrelationContext : ICorrelationContext
    {
        public string CorrelationId => "admin-support-correlation";
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

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        private readonly List<ManagedDocument> _documents = [];
        public Guid LastFailedOrganizationId { get; private set; }

        public void Add(ManagedDocument document) => _documents.Add(document);

        public Task<IReadOnlyList<ManagedDocument>> FindFailedDocumentsAsync(Guid organizationId, int limit, CancellationToken ct = default)
        {
            LastFailedOrganizationId = organizationId;
            return Task.FromResult<IReadOnlyList<ManagedDocument>>(
                _documents
                    .Where(doc => doc.OrganizationId == organizationId && doc.ProcessingStatus == DocumentProcessingStatus.Failed)
                    .Take(limit)
                    .ToArray());
        }

        public Task<IReadOnlyList<ManagedDocument>> ListAsync(Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ManagedDocument>>([]);

        public Task<ManagedDocument?> FindAsync(Guid documentId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);

        public Task<ManagedDocument> CreateAsync(NewManagedDocument document, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<DocumentDisableResult?> DisableRetrievalAsync(Guid documentId, Guid organizationId, DateTimeOffset updatedAt, CancellationToken ct = default) =>
            Task.FromResult<DocumentDisableResult?>(null);

        public Task<DocumentEnableResult?> EnableRetrievalAsync(Guid documentId, Guid organizationId, DateTimeOffset updatedAt, CancellationToken ct = default) =>
            Task.FromResult<DocumentEnableResult?>(null);

        public Task<IReadOnlyList<ManagedDocument>> FindPendingForProcessingAsync(int maxCount, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ManagedDocument>>([]);

        public Task<ManagedDocument?> ClaimForProcessingAsync(Guid documentId, DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);

        public Task<ManagedDocument?> MarkProcessedAsync(Guid documentId, DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);

        public Task<ManagedDocument?> MarkFailedAsync(Guid documentId, string safeFailureReason, DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        private readonly List<StoredAuditLogEntry> _entries = [];
        public Guid LastOrganizationId { get; private set; }

        public void Add(StoredAuditLogEntry entry) => _entries.Add(entry);

        public Task<IReadOnlyList<AuditLogResult>> FindAsync(
            Guid organizationId,
            AuditLogQuery query,
            int limit,
            CancellationToken ct = default)
        {
            LastOrganizationId = organizationId;
            var rows = _entries
                .Where(entry => entry.OrganizationId == organizationId)
                .Where(entry => !query.From.HasValue || entry.CreatedAt >= query.From.Value)
                .Where(entry => !query.To.HasValue || entry.CreatedAt <= query.To.Value)
                .Where(entry => string.IsNullOrWhiteSpace(query.EventType) || entry.EventType == query.EventType)
                .OrderByDescending(entry => entry.CreatedAt)
                .Take(limit)
                .Select(entry => new AuditLogResult(
                    entry.AuditLogEntryId,
                    entry.EventType,
                    entry.Message,
                    entry.Severity,
                    entry.UserId,
                    entry.EntityType,
                    entry.EntityId,
                    entry.CorrelationId,
                    entry.CreatedAt))
                .ToArray();

            return Task.FromResult<IReadOnlyList<AuditLogResult>>(rows);
        }
    }

    private sealed record StoredAuditLogEntry(
        Guid AuditLogEntryId,
        Guid OrganizationId,
        Guid? UserId,
        string EventType,
        string Message,
        string Severity,
        string? EntityType,
        Guid? EntityId,
        string? CorrelationId,
        DateTimeOffset CreatedAt);
}

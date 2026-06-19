using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeOps.Application.Admin;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeOps.Api.Tests.Admin;

public sealed class AdminControllerTests : IClassFixture<AdminApiTestFactory>
{
    private readonly AdminApiTestFactory _factory;

    public AdminControllerTests(AdminApiTestFactory factory)
    {
        _factory = factory;
        _factory.Reset();
    }

    [Fact]
    public async Task ProcessingFailures_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/admin/processing-failures");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(AdminApiTestFactory.AgentEmail)]
    [InlineData(AdminApiTestFactory.SupervisorEmail)]
    [InlineData(AdminApiTestFactory.ManagerEmail)]
    public async Task ProcessingFailures_DeniedRoles_Return403(string email)
    {
        var client = await AuthenticateAsync(email);
        var response = await client.GetAsync("/api/v1/admin/processing-failures");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(AdminApiTestFactory.KnowledgeAdminEmail)]
    [InlineData(AdminApiTestFactory.AdminEmail)]
    public async Task ProcessingFailures_AllowedRoles_Return200(string email)
    {
        var client = await AuthenticateAsync(email);
        var response = await client.GetAsync("/api/v1/admin/processing-failures");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProcessingFailures_OrgA_DoesNotIncludeOrgBDocuments()
    {
        _factory.Documents.Add(AdminApiTestFactory.MakeFailedDocument(AdminApiTestFactory.OrgId, "Org A failure"));
        _factory.Documents.Add(AdminApiTestFactory.MakeFailedDocument(AdminApiTestFactory.OtherOrgId, "Org B failure"));
        var client = await AuthenticateAsync(AdminApiTestFactory.KnowledgeAdminEmail);

        var text = await (await client.GetAsync("/api/v1/admin/processing-failures")).Content.ReadAsStringAsync();

        Assert.Contains("Org A failure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Org B failure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("storageLocation", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("local://", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw exception", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack trace", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuditLog_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/admin/audit-log");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(AdminApiTestFactory.AgentEmail)]
    [InlineData(AdminApiTestFactory.SupervisorEmail)]
    [InlineData(AdminApiTestFactory.KnowledgeAdminEmail)]
    [InlineData(AdminApiTestFactory.ManagerEmail)]
    public async Task AuditLog_DeniedRoles_Return403(string email)
    {
        var client = await AuthenticateAsync(email);
        var response = await client.GetAsync("/api/v1/admin/audit-log");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuditLog_Admin_Returns200()
    {
        var client = await AuthenticateAsync(AdminApiTestFactory.AdminEmail);
        var response = await client.GetAsync("/api/v1/admin/audit-log");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuditLog_OrgA_DoesNotIncludeOrgBEntries()
    {
        _factory.AuditLog.Add(AdminApiTestFactory.MakeAuditLog(AdminApiTestFactory.OrgId, "DocumentUploadAccepted", "Org A row"));
        _factory.AuditLog.Add(AdminApiTestFactory.MakeAuditLog(AdminApiTestFactory.OtherOrgId, "DocumentUploadAccepted", "Org B row"));
        var client = await AuthenticateAsync(AdminApiTestFactory.AdminEmail);

        var text = await (await client.GetAsync("/api/v1/admin/audit-log")).Content.ReadAsStringAsync();

        Assert.Contains("Org A row", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Org B row", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuditLog_FiltersByEventTypeAndDate()
    {
        _factory.AuditLog.Add(AdminApiTestFactory.MakeAuditLog(
            AdminApiTestFactory.OrgId,
            "DocumentUploadAccepted",
            "Included row",
            AdminApiTestFactory.Now.AddHours(-1)));
        _factory.AuditLog.Add(AdminApiTestFactory.MakeAuditLog(
            AdminApiTestFactory.OrgId,
            "DocumentProcessingFailed",
            "Wrong type",
            AdminApiTestFactory.Now.AddHours(-1)));
        var client = await AuthenticateAsync(AdminApiTestFactory.AdminEmail);

        var text = await (await client.GetAsync(
            "/api/v1/admin/audit-log?from=2026-06-01T00:00:00Z&to=2026-06-01T23:59:59Z&eventType=DocumentUploadAccepted"))
            .Content.ReadAsStringAsync();

        Assert.Contains("Included row", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Wrong type", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuditLog_EmitsAuditLogViewedEvent()
    {
        var client = await AuthenticateAsync(AdminApiTestFactory.AdminEmail);
        _factory.AuditEvents.Reset();

        await client.GetAsync("/api/v1/admin/audit-log");

        Assert.Contains(
            _factory.AuditEvents.Events,
            e => e.EventType == AuditEventTypes.AuditLogViewed
                && e.OrganizationId == AdminApiTestFactory.OrgId);
    }

    [Fact]
    public async Task AuditLog_ResponseDoesNotContainProhibitedContent()
    {
        _factory.AuditLog.Add(AdminApiTestFactory.MakeAuditLog(
            AdminApiTestFactory.OrgId,
            "DocumentUploadAccepted",
            "Document upload accepted. DocumentId=bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb."));
        var client = await AuthenticateAsync(AdminApiTestFactory.AdminEmail);

        var text = await (await client.GetAsync("/api/v1/admin/audit-log")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bearer", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("api key", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection string", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prompt", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chunk text", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("question text", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answer text", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider payload", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack trace", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuditLogViewed_MessageDoesNotIncludeReturnedAuditRows()
    {
        _factory.AuditLog.Add(AdminApiTestFactory.MakeAuditLog(
            AdminApiTestFactory.OrgId,
            "DocumentUploadAccepted",
            "Returned audit row should not be copied into viewed event."));
        var client = await AuthenticateAsync(AdminApiTestFactory.AdminEmail);
        _factory.AuditEvents.Reset();

        await client.GetAsync("/api/v1/admin/audit-log");

        var evt = Assert.Single(_factory.AuditEvents.Events, e => e.EventType == AuditEventTypes.AuditLogViewed);
        Assert.DoesNotContain("Returned audit row", evt.Message, StringComparison.Ordinal);
    }

    private async Task<HttpClient> AuthenticateAsync(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = AdminApiTestFactory.Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.GetProperty("accessToken").GetString());
        return client;
    }
}

public sealed class AdminApiTestFactory : WebApplicationFactory<Program>
{
    public const string AgentEmail = "agent.admin@example.test";
    public const string SupervisorEmail = "supervisor.admin@example.test";
    public const string KnowledgeAdminEmail = "kadmin.admin@example.test";
    public const string ManagerEmail = "manager.admin@example.test";
    public const string AdminEmail = "admin.admin@example.test";
    public const string Password = "test-password";

    public static readonly Guid OrgId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid OtherOrgId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    public static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    public FakeDocumentRepository Documents { get; } = new();
    public FakeAuditLogRepository AuditLog { get; } = new();
    public RecordingAuditEventWriter AuditEvents { get; } = new();

    public void Reset()
    {
        Documents.Reset();
        AuditLog.Reset();
        AuditEvents.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "KnowledgeOps",
                ["Jwt:Audience"] = "KnowledgeOps",
                ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-characters-long",
                ["Jwt:ExpirationMinutes"] = "60",
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=localhost;Database=TestDb;Trusted_Connection=True;"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserAuthRepository>();
            services.AddScoped<IUserAuthRepository, FakeAuthRepository>();
            services.RemoveAll<IUserAccessStateReader>();
            services.AddScoped<IUserAccessStateReader>(sp =>
                new FixedAccessStateReader(sp.GetRequiredService<IUserAuthRepository>()));
            services.RemoveAll<IDocumentRepository>();
            services.AddSingleton<IDocumentRepository>(Documents);
            services.RemoveAll<IAuditLogRepository>();
            services.AddSingleton<IAuditLogRepository>(AuditLog);
            services.RemoveAll<IPasswordHasher>();
            services.AddSingleton<IPasswordHasher, TestPasswordHasher>();
            services.RemoveAll<IAuditEventWriter>();
            services.AddSingleton<IAuditEventWriter>(AuditEvents);
        });
    }

    public static ManagedDocument MakeFailedDocument(Guid organizationId, string failureReason) =>
        new(
            Guid.NewGuid(),
            organizationId,
            "document.pdf",
            "Example policy",
            "application/pdf",
            100,
            "local://must-not-leak",
            DocumentProcessingStatus.Failed,
            failureReason,
            false,
            UserId,
            Now.AddDays(-1),
            Now.AddHours(-2),
            null,
            Now.AddDays(-1),
            Now,
            null);

    public static ScopedAuditLogEntry MakeAuditLog(
        Guid organizationId,
        string eventType,
        string message,
        DateTimeOffset? createdAt = null) =>
        new(
            organizationId,
            Guid.NewGuid(),
            eventType,
            message,
            "Info",
            UserId,
            "Document",
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "safe-correlation",
            createdAt ?? Now);

    public sealed class FakeDocumentRepository : IDocumentRepository
    {
        private readonly List<ManagedDocument> _documents = [];

        public void Reset() => _documents.Clear();
        public void Add(ManagedDocument document) => _documents.Add(document);

        public Task<IReadOnlyList<ManagedDocument>> FindFailedDocumentsAsync(Guid organizationId, int limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ManagedDocument>>(
                _documents
                    .Where(doc => doc.OrganizationId == organizationId && doc.ProcessingStatus == DocumentProcessingStatus.Failed)
                    .Take(limit)
                    .ToArray());

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

    public sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        private readonly List<ScopedAuditLogEntry> _entries = [];

        public void Reset() => _entries.Clear();

        public void Add(ScopedAuditLogEntry entry) => _entries.Add(entry);

        public Task<IReadOnlyList<AuditLogResult>> FindAsync(Guid organizationId, AuditLogQuery query, int limit, CancellationToken ct = default)
        {
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

    public sealed class RecordingAuditEventWriter : IAuditEventWriter
    {
        public List<AuditEvent> Events { get; } = [];

        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public void Reset() => Events.Clear();
    }

    private sealed class FakeAuthRepository : IUserAuthRepository
    {
        private static readonly Dictionary<string, UserAuthRecord> Users =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [AgentEmail] = User(0xA1, AgentEmail, "Agent", ["Agent"]),
                [SupervisorEmail] = User(0xA2, SupervisorEmail, "Supervisor", ["Supervisor"]),
                [KnowledgeAdminEmail] = User(0xA3, KnowledgeAdminEmail, "Knowledge Admin", ["KnowledgeAdmin"]),
                [ManagerEmail] = User(0xA4, ManagerEmail, "Manager", ["Manager"]),
                [AdminEmail] = User(0xA5, AdminEmail, "Admin", ["Admin"])
            };

        public Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(Users.GetValueOrDefault(email));

        public Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Users.Values.SingleOrDefault(user => user.UserId == userId));

        public Task UpdateLastLoginAtAsync(Guid userId, DateTimeOffset loginAt, CancellationToken ct = default) =>
            Task.CompletedTask;

        private static UserAuthRecord User(byte suffix, string email, string displayName, IReadOnlyList<string> roles) =>
            new(
                Guid.Parse($"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaa{suffix:x2}"),
                OrgId,
                email,
                displayName,
                Password,
                UserStatus.Active,
                roles);
    }

    private sealed class FixedAccessStateReader(IUserAuthRepository repository) : IUserAccessStateReader
    {
        public async Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await repository.FindByIdAsync(userId, ct);
            if (user is null || user.Status != UserStatus.Active)
                return null;
            return new UserAccessState(user.UserId, user.OrganizationId, user.Roles);
        }
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => password;
        public bool VerifyPassword(string hashedPassword, string password) => hashedPassword == password;
    }

    public sealed record ScopedAuditLogEntry(
        Guid OrganizationId,
        Guid AuditLogEntryId,
        string EventType,
        string Message,
        string Severity,
        Guid? UserId,
        string? EntityType,
        Guid? EntityId,
        string? CorrelationId,
        DateTimeOffset CreatedAt);
}

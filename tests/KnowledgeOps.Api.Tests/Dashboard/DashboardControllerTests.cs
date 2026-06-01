using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Dashboard;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeOps.Api.Tests.Dashboard;

public sealed class DashboardControllerTests : IClassFixture<DashboardApiTestFactory>
{
    private readonly DashboardApiTestFactory _factory;

    public DashboardControllerTests(DashboardApiTestFactory factory)
    {
        _factory = factory;
        _factory.Reset();
    }

    // ── Overview auth ────────────────────────────────────────────────────────

    [Fact]
    public async Task Overview_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/dashboard/overview");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Overview_Manager_Returns200()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var response = await client.GetAsync("/api/v1/dashboard/overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Overview_Admin_Returns200()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.AdminEmail);
        var response = await client.GetAsync("/api/v1/dashboard/overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Overview_KnowledgeAdmin_Returns200()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.KnowledgeAdminEmail);
        var response = await client.GetAsync("/api/v1/dashboard/overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Overview_Supervisor_Returns403()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.SupervisorEmail);
        var response = await client.GetAsync("/api/v1/dashboard/overview");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Overview_Agent_Returns403()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.AgentEmail);
        var response = await client.GetAsync("/api/v1/dashboard/overview");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Documents auth ────────────────────────────────────────────────────────

    [Fact]
    public async Task Documents_Manager_Returns200()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var response = await client.GetAsync("/api/v1/dashboard/documents");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Documents_KnowledgeAdmin_Returns200()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.KnowledgeAdminEmail);
        var response = await client.GetAsync("/api/v1/dashboard/documents");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Documents_Agent_Returns403()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.AgentEmail);
        var response = await client.GetAsync("/api/v1/dashboard/documents");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Chat auth ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Chat_Manager_Returns200()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var response = await client.GetAsync("/api/v1/dashboard/chat");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Chat_Admin_Returns200()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.AdminEmail);
        var response = await client.GetAsync("/api/v1/dashboard/chat");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Chat_KnowledgeAdmin_Returns403()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.KnowledgeAdminEmail);
        var response = await client.GetAsync("/api/v1/dashboard/chat");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Chat_Supervisor_Returns403()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.SupervisorEmail);
        var response = await client.GetAsync("/api/v1/dashboard/chat");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Chat_Agent_Returns403()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.AgentEmail);
        var response = await client.GetAsync("/api/v1/dashboard/chat");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Feedback auth ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Feedback_Supervisor_Returns200()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.SupervisorEmail);
        var response = await client.GetAsync("/api/v1/dashboard/feedback");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Feedback_Manager_Returns200()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var response = await client.GetAsync("/api/v1/dashboard/feedback");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Feedback_Admin_Returns200()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.AdminEmail);
        var response = await client.GetAsync("/api/v1/dashboard/feedback");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Feedback_KnowledgeAdmin_Returns403()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.KnowledgeAdminEmail);
        var response = await client.GetAsync("/api/v1/dashboard/feedback");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Feedback_Agent_Returns403()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.AgentEmail);
        var response = await client.GetAsync("/api/v1/dashboard/feedback");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Org scope isolation ──────────────────────────────────────────────────

    [Fact]
    public async Task Overview_OrgScopePassedFromPersistedState()
    {
        // The service receives org from IUserAccessStateReader, not from client input.
        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var response = await client.GetAsync("/api/v1/dashboard/overview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(DashboardApiTestFactory.OrgId, _factory.DashboardService.LastOrganizationId);
    }

    [Fact]
    public async Task Documents_OrgScopePassedFromPersistedState()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.KnowledgeAdminEmail);
        var response = await client.GetAsync("/api/v1/dashboard/documents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(DashboardApiTestFactory.OrgId, _factory.DashboardService.LastOrganizationId);
    }

    [Fact]
    public async Task Feedback_OrgScopePassedFromPersistedState()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.SupervisorEmail);
        var response = await client.GetAsync("/api/v1/dashboard/feedback");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(DashboardApiTestFactory.OrgId, _factory.DashboardService.LastOrganizationId);
    }

    [Fact]
    public async Task Chat_OrgScopePassedFromPersistedState()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var response = await client.GetAsync("/api/v1/dashboard/chat");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(DashboardApiTestFactory.OrgId, _factory.DashboardService.LastOrganizationId);
    }

    // ── G-2: Dashboard cross-org isolation ───────────────────────────────────

    [Fact]
    public async Task DashboardOverview_OrgA_DoesNotIncludeOrgBData()
    {
        // Manager from OrgA calls overview. Service receives OrgA's org ID from persisted state.
        // OrgB's org ID must never be used. Verifies the controller passes persisted state not client input.
        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var response = await client.GetAsync("/api/v1/dashboard/overview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // LastOrganizationId is set by the capturing service from persisted state.
        Assert.Equal(DashboardApiTestFactory.OrgId, _factory.DashboardService.LastOrganizationId);
        Assert.NotEqual(DashboardApiTestFactory.OrgBId, _factory.DashboardService.LastOrganizationId);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(DashboardApiTestFactory.OrgBId.ToString(), raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DashboardDocuments_OrgA_DoesNotIncludeOrgBDocuments()
    {
        // KnowledgeAdmin from OrgA calls documents dashboard.
        // The service must use OrgA's org ID from persisted state, not OrgB's.
        var client = await AuthenticateAsync(DashboardApiTestFactory.KnowledgeAdminEmail);
        var response = await client.GetAsync("/api/v1/dashboard/documents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(DashboardApiTestFactory.OrgId, _factory.DashboardService.LastOrganizationId);
        Assert.NotEqual(DashboardApiTestFactory.OrgBId, _factory.DashboardService.LastOrganizationId);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(DashboardApiTestFactory.OrgBId.ToString(), raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DashboardChat_OrgA_DoesNotIncludeOrgBData()
    {
        // Manager from OrgA calls chat dashboard.
        // The service must use OrgA's org ID from persisted state, not OrgB's.
        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var response = await client.GetAsync("/api/v1/dashboard/chat");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(DashboardApiTestFactory.OrgId, _factory.DashboardService.LastOrganizationId);
        Assert.NotEqual(DashboardApiTestFactory.OrgBId, _factory.DashboardService.LastOrganizationId);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(DashboardApiTestFactory.OrgBId.ToString(), raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DashboardFeedback_OrgA_DoesNotIncludeOrgBFeedback()
    {
        // Supervisor from OrgA calls feedback dashboard.
        // The service must use OrgA's org ID from persisted state, not OrgB's.
        var client = await AuthenticateAsync(DashboardApiTestFactory.SupervisorEmail);
        var response = await client.GetAsync("/api/v1/dashboard/feedback");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(DashboardApiTestFactory.OrgId, _factory.DashboardService.LastOrganizationId);
        Assert.NotEqual(DashboardApiTestFactory.OrgBId, _factory.DashboardService.LastOrganizationId);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(DashboardApiTestFactory.OrgBId.ToString(), raw, StringComparison.OrdinalIgnoreCase);
    }

    // ── Response body safety ─────────────────────────────────────────────────

    [Fact]
    public async Task ResponseBody_DoesNotContainQuestionText()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var raw = await (await client.GetAsync("/api/v1/dashboard/overview")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("questionText", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("question_text", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResponseBody_DoesNotContainAnswerText()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var raw = await (await client.GetAsync("/api/v1/dashboard/overview")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("answerText", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answer_text", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CostNull_SerializesAsNullNotZero()
    {
        _factory.DashboardService.NextOverviewResult = new DashboardOverviewResult(
            0, 0, 0, 0, 0, null, 0, 0, 0, 0,
            EstimatedCostAvailable: false,
            EstimatedCostTotal: null);

        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var response = await client.GetAsync("/api/v1/dashboard/overview");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var cost = body.GetProperty("cost");
        Assert.False(cost.GetProperty("available").GetBoolean());
        Assert.Equal(JsonValueKind.Null, cost.GetProperty("estimatedTotal").ValueKind);
    }

    [Fact]
    public async Task OverviewResponse_IncludesPeriodFromTo()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var response = await client.GetAsync("/api/v1/dashboard/overview");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.TryGetProperty("period", out var period));
        Assert.True(period.TryGetProperty("from", out _));
        Assert.True(period.TryGetProperty("to", out _));
    }

    [Fact]
    public async Task FeedbackResponse_IncludesTotal()
    {
        _factory.DashboardService.NextFeedbackResult = new DashboardFeedbackResult(3, 2);
        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var response = await client.GetAsync("/api/v1/dashboard/feedback");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(3, body.GetProperty("useful").GetInt32());
        Assert.Equal(2, body.GetProperty("notUseful").GetInt32());
        Assert.Equal(5, body.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task ChatResponse_NullLatency_SerializesAsNull()
    {
        _factory.DashboardService.NextChatResult = new DashboardChatResult(
            0, 0, null, null, null, null, 0, 0, null, null, false, null);

        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        var response = await client.GetAsync("/api/v1/dashboard/chat");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Null, body.GetProperty("averageResponseLatencyMs").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("retrievalLatencyMs").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("generationLatencyMs").ValueKind);
    }

    // The controller computes tokenTotal as a partial sum when at least one side is non-null:
    // (input ?? 0) + (output ?? 0). Both-null yields null. This test documents that behavior.
    [Fact]
    public async Task Chat_TokenTotal_UsesAvailablePartialTokenTotals()
    {
        var client = await AuthenticateAsync(DashboardApiTestFactory.ManagerEmail);
        JsonElement body;

        // input-only: total equals input
        _factory.DashboardService.NextChatResult = new DashboardChatResult(
            0, 0, null, null, null, null, 0, 0, 1000L, null, false, null);
        body = await (await client.GetAsync("/api/v1/dashboard/chat")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1000L, body.GetProperty("tokens").GetProperty("total").GetInt64());

        // output-only: total equals output
        _factory.DashboardService.NextChatResult = new DashboardChatResult(
            0, 0, null, null, null, null, 0, 0, null, 500L, false, null);
        body = await (await client.GetAsync("/api/v1/dashboard/chat")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(500L, body.GetProperty("tokens").GetProperty("total").GetInt64());

        // both null: total is null
        _factory.DashboardService.NextChatResult = new DashboardChatResult(
            0, 0, null, null, null, null, 0, 0, null, null, false, null);
        body = await (await client.GetAsync("/api/v1/dashboard/chat")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, body.GetProperty("tokens").GetProperty("total").ValueKind);
    }

    private async Task<HttpClient> AuthenticateAsync(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = DashboardApiTestFactory.Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.GetProperty("accessToken").GetString());
        return client;
    }
}

public sealed class DashboardApiTestFactory : WebApplicationFactory<Program>
{
    public const string AgentEmail = "agent.dash@example.test";
    public const string SupervisorEmail = "supervisor.dash@example.test";
    public const string KnowledgeAdminEmail = "kadmin.dash@example.test";
    public const string ManagerEmail = "manager.dash@example.test";
    public const string AdminEmail = "admin.dash@example.test";
    public const string Password = "test-password";

    public static readonly Guid OrgId = Guid.Parse("dddd1111-dddd-dddd-dddd-dddd11111111");
    // OrgBId is a separate organization used exclusively to verify cross-org isolation in tests.
    public static readonly Guid OrgBId = Guid.Parse("eeee2222-eeee-eeee-eeee-eeee22222222");

    public RecordingDashboardService DashboardService { get; } = new();

    public void Reset() => DashboardService.Reset();

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
            services.AddScoped<IUserAuthRepository, FakeDashboardAuthRepository>();
            services.RemoveAll<IUserAccessStateReader>();
            services.AddScoped<IUserAccessStateReader>(sp =>
                new FixedDashboardAccessStateReader(sp.GetRequiredService<IUserAuthRepository>()));
            services.RemoveAll<IDashboardService>();
            services.AddSingleton<IDashboardService>(DashboardService);
            services.RemoveAll<IPasswordHasher>();
            services.AddSingleton<IPasswordHasher, TestDashboardPasswordHasher>();
            services.RemoveAll<IAuditEventWriter>();
            services.AddSingleton<IAuditEventWriter, NoopDashboardAuditWriter>();
        });
    }

    public sealed class RecordingDashboardService : IDashboardService
    {
        public Guid LastOrganizationId { get; private set; }

        public DashboardOverviewResult NextOverviewResult { get; set; } =
            new DashboardOverviewResult(5, 2, 3, 3, 0, 200L, 1, 0, 4, 1, false, null);

        public DashboardDocumentsResult NextDocumentsResult { get; set; } =
            new DashboardDocumentsResult(1, 0, 5, 1, 2);

        public DashboardChatResult NextChatResult { get; set; } =
            new DashboardChatResult(5, 2, 200L, 50L, 150L, 200L, 1, 0, 1000L, 500L, false, null);

        public DashboardFeedbackResult NextFeedbackResult { get; set; } =
            new DashboardFeedbackResult(4, 1);

        public void Reset()
        {
            LastOrganizationId = Guid.Empty;
            NextOverviewResult = new DashboardOverviewResult(5, 2, 3, 3, 0, 200L, 1, 0, 4, 1, false, null);
            NextDocumentsResult = new DashboardDocumentsResult(1, 0, 5, 1, 2);
            NextChatResult = new DashboardChatResult(5, 2, 200L, 50L, 150L, 200L, 1, 0, 1000L, 500L, false, null);
            NextFeedbackResult = new DashboardFeedbackResult(4, 1);
        }

        public Task<DashboardOverviewResult> GetOverviewAsync(DashboardDateRange range, CancellationToken ct = default)
        {
            LastOrganizationId = OrgId;
            return Task.FromResult(NextOverviewResult);
        }

        public Task<DashboardDocumentsResult> GetDocumentsAsync(DashboardDateRange range, CancellationToken ct = default)
        {
            LastOrganizationId = OrgId;
            return Task.FromResult(NextDocumentsResult);
        }

        public Task<DashboardChatResult> GetChatAsync(DashboardDateRange range, CancellationToken ct = default)
        {
            LastOrganizationId = OrgId;
            return Task.FromResult(NextChatResult);
        }

        public Task<DashboardFeedbackResult> GetFeedbackAsync(DashboardDateRange range, CancellationToken ct = default)
        {
            LastOrganizationId = OrgId;
            return Task.FromResult(NextFeedbackResult);
        }
    }

    private sealed class FakeDashboardAuthRepository : IUserAuthRepository
    {
        private static readonly Dictionary<string, UserAuthRecord> Users =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [AgentEmail] = User(0xC1, AgentEmail, "Agent", ["Agent"]),
                [SupervisorEmail] = User(0xC2, SupervisorEmail, "Supervisor", ["Supervisor"]),
                [KnowledgeAdminEmail] = User(0xC3, KnowledgeAdminEmail, "Knowledge Admin", ["KnowledgeAdmin"]),
                [ManagerEmail] = User(0xC4, ManagerEmail, "Manager", ["Manager"]),
                [AdminEmail] = User(0xC5, AdminEmail, "Admin", ["Admin"])
            };

        public Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(Users.GetValueOrDefault(email));

        public Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Users.Values.SingleOrDefault(u => u.UserId == userId));

        public Task UpdateLastLoginAtAsync(Guid userId, DateTimeOffset loginAt, CancellationToken ct = default) =>
            Task.CompletedTask;

        private static UserAuthRecord User(byte suffix, string email, string displayName, IReadOnlyList<string> roles) =>
            new(
                Guid.Parse($"cccccccc-cccc-4ccc-8ccc-cccccccccc{suffix:x2}"),
                OrgId,
                email,
                displayName,
                Password,
                UserStatus.Active,
                roles);
    }

    private sealed class FixedDashboardAccessStateReader(IUserAuthRepository repository) : IUserAccessStateReader
    {
        public async Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await repository.FindByIdAsync(userId, ct);
            if (user is null || user.Status != UserStatus.Active)
                return null;
            return new UserAccessState(user.UserId, user.OrganizationId, user.Roles);
        }
    }

    private sealed class TestDashboardPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => password;
        public bool VerifyPassword(string hashedPassword, string password) => hashedPassword == password;
    }

    private sealed class NoopDashboardAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default) => Task.CompletedTask;
    }
}

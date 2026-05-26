using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeOps.Api.Tests.Authorization;

public sealed class AuthorizationApiTests : IClassFixture<AuthorizationApiTestFactory>
{
    private readonly AuthorizationApiTestFactory _factory;

    public AuthorizationApiTests(AuthorizationApiTestFactory factory)
    {
        _factory = factory;
    }

    // ── Unauthenticated denial (401) ───────────────────────────────────────────

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/test/authorization/documents-upload");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ScopeCheck_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/api/test/authorization/scope-check?targetOrgId={AuthorizationApiTestFactory.AsteriaOrgId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Agent allowed (Chat.AskQuestion) ──────────────────────────────────────

    [Fact]
    public async Task ChatAsk_AsAgent_Returns200()
    {
        var token = await GetTokenAsync(AuthorizationApiTestFactory.AgentEmail);
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/test/authorization/chat-ask");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Agent denied Documents.Upload (403) ───────────────────────────────────

    [Fact]
    public async Task DocumentsUpload_AsAgent_Returns403()
    {
        var token = await GetTokenAsync(AuthorizationApiTestFactory.AgentEmail);
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/test/authorization/documents-upload");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── KnowledgeAdmin allowed Documents.Upload ────────────────────────────────

    [Fact]
    public async Task DocumentsUpload_AsKnowledgeAdmin_Returns200()
    {
        var token = await GetTokenAsync(AuthorizationApiTestFactory.KnowledgeAdminEmail);
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/test/authorization/documents-upload");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Manager denied Documents.Upload (403) ─────────────────────────────────

    [Fact]
    public async Task DocumentsUpload_AsManager_Returns403()
    {
        var token = await GetTokenAsync(AuthorizationApiTestFactory.ManagerEmail);
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/test/authorization/documents-upload");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Manager allowed Dashboard.ViewOverview ─────────────────────────────────

    [Fact]
    public async Task DashboardOverview_AsManager_Returns200()
    {
        var token = await GetTokenAsync(AuthorizationApiTestFactory.ManagerEmail);
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/test/authorization/dashboard-overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Admin allowed Users.View ───────────────────────────────────────────────

    [Fact]
    public async Task UsersView_AsAdmin_Returns200()
    {
        var token = await GetTokenAsync(AuthorizationApiTestFactory.AsteriaAdminEmail);
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/test/authorization/users-view");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Non-Admin denied Users.View (403) ─────────────────────────────────────

    [Theory]
    [InlineData(AuthorizationApiTestFactory.AgentEmail)]
    [InlineData(AuthorizationApiTestFactory.SupervisorEmail)]
    [InlineData(AuthorizationApiTestFactory.KnowledgeAdminEmail)]
    [InlineData(AuthorizationApiTestFactory.ManagerEmail)]
    public async Task UsersView_AsNonAdmin_Returns403(string email)
    {
        var token = await GetTokenAsync(email);
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/test/authorization/users-view");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Cross-organization scope — 404 ────────────────────────────────────────

    [Fact]
    public async Task ScopeCheck_CrossOrganization_Returns404()
    {
        // Asteria agent tries to access a resource scoped to Boreal org.
        var token = await GetTokenAsync(AuthorizationApiTestFactory.AgentEmail);
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync(
            $"/api/test/authorization/scope-check?targetOrgId={AuthorizationApiTestFactory.BorealOrgId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ScopeCheck_SameOrganization_Returns200()
    {
        var token = await GetTokenAsync(AuthorizationApiTestFactory.AgentEmail);
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync(
            $"/api/test/authorization/scope-check?targetOrgId={AuthorizationApiTestFactory.AsteriaOrgId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScopeCheck_AdminCrossOrganization_Returns404()
    {
        // Admin from Asteria also cannot access Boreal (ADR-010: Admin is same-org only).
        var token = await GetTokenAsync(AuthorizationApiTestFactory.AsteriaAdminEmail);
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync(
            $"/api/test/authorization/scope-check?targetOrgId={AuthorizationApiTestFactory.BorealOrgId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Safe denial response — no sensitive data leakage ─────────────────────

    [Fact]
    public async Task DeniedResponse_DoesNotExposePermissionNameInBody()
    {
        var token = await GetTokenAsync(AuthorizationApiTestFactory.AgentEmail);
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/test/authorization/documents-upload");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("Documents.Upload", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("permission", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeniedResponse_DoesNotExposeOrganizationNameInBody()
    {
        var token = await GetTokenAsync(AuthorizationApiTestFactory.AgentEmail);
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/test/authorization/users-view");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("Asteria", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Boreal", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeniedResponse_DoesNotExposeBearerTokenInBody()
    {
        var token = await GetTokenAsync(AuthorizationApiTestFactory.AgentEmail);
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/test/authorization/users-view");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("Bearer", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(token, body);
    }

    // ── Direct API bypass — backend must enforce despite what frontend hides ──

    [Fact]
    public async Task DirectApiCall_AsAgent_DocumentsUpload_Returns403_WithoutUiFlow()
    {
        // This test proves that the backend enforces authorization on direct HTTP calls,
        // independent of any frontend visibility logic (UX-only per docs/16 Section 8.1).
        var token = await GetTokenAsync(AuthorizationApiTestFactory.AgentEmail);

        using var request = new HttpRequestMessage(HttpMethod.Get,
            "/api/test/authorization/documents-upload");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var client = _factory.CreateClient();
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<string> GetTokenAsync(string email)
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password = AuthorizationApiTestFactory.Password });
        loginResponse.EnsureSuccessStatusCode();
        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public sealed class AuthorizationApiTestFactory : WebApplicationFactory<Program>
{
    // Asteria users (one per role)
    public const string AgentEmail = "agent@asteria.test";
    public const string SupervisorEmail = "supervisor@asteria.test";
    public const string KnowledgeAdminEmail = "knowledgeadmin@asteria.test";
    public const string ManagerEmail = "manager@asteria.test";
    public const string AsteriaAdminEmail = "admin@asteria.test";

    // Boreal user (for cross-org tests)
    public const string BorealAdminEmail = "admin@boreal.test";

    public const string Password = "test-password";

    // Org IDs match SeedDataIds.cs — kept inline to avoid Infrastructure project reference in Api.Tests.
    public static readonly Guid AsteriaOrgId = new("11111111-1111-4111-8111-111111111111");
    public static readonly Guid BorealOrgId = new("22222222-2222-4222-8222-222222222222");

    private static readonly Guid AsteriaAgentId = Guid.NewGuid();
    private static readonly Guid AsteriaSupervisorId = Guid.NewGuid();
    private static readonly Guid AsteriaKnowledgeAdminId = Guid.NewGuid();
    private static readonly Guid AsteriaManagerId = Guid.NewGuid();
    private static readonly Guid AsteriaAdminId = Guid.NewGuid();
    private static readonly Guid BorealAdminId = Guid.NewGuid();

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
                    "Server=localhost;Database=TestDb;Trusted_Connection=True;",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserAuthRepository>();
            services.AddScoped<IUserAuthRepository>(_ => new FakeMultiRoleRepository());

            services.RemoveAll<IPasswordHasher>();
            services.AddSingleton<IPasswordHasher, PlaintextPasswordHasher>();

            // Register test-only controller from this assembly.
            services.AddControllers()
                .AddApplicationPart(typeof(AuthorizationApiTestFactory).Assembly);
        });
    }

    private sealed class FakeMultiRoleRepository : IUserAuthRepository
    {
        private static readonly Dictionary<string, UserAuthRecord> ByEmail =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [AgentEmail] = new UserAuthRecord(AsteriaAgentId, AuthorizationApiTestFactory.AsteriaOrgId,
                    AgentEmail, "Asteria Agent", Password, UserStatus.Active, ["Agent"]),

                [SupervisorEmail] = new UserAuthRecord(AsteriaSupervisorId, AuthorizationApiTestFactory.AsteriaOrgId,
                    SupervisorEmail, "Asteria Supervisor", Password, UserStatus.Active, ["Supervisor"]),

                [KnowledgeAdminEmail] = new UserAuthRecord(AsteriaKnowledgeAdminId, AuthorizationApiTestFactory.AsteriaOrgId,
                    KnowledgeAdminEmail, "Asteria KnowledgeAdmin", Password, UserStatus.Active, ["KnowledgeAdmin"]),

                [ManagerEmail] = new UserAuthRecord(AsteriaManagerId, AuthorizationApiTestFactory.AsteriaOrgId,
                    ManagerEmail, "Asteria Manager", Password, UserStatus.Active, ["Manager"]),

                [AsteriaAdminEmail] = new UserAuthRecord(AsteriaAdminId, AuthorizationApiTestFactory.AsteriaOrgId,
                    AsteriaAdminEmail, "Asteria Admin", Password, UserStatus.Active, ["Admin"]),

                [BorealAdminEmail] = new UserAuthRecord(BorealAdminId, AuthorizationApiTestFactory.BorealOrgId,
                    BorealAdminEmail, "Boreal Admin", Password, UserStatus.Active, ["Admin"]),
            };

        private static readonly Dictionary<Guid, UserAuthRecord> ById =
            ByEmail.Values.ToDictionary(u => u.UserId);

        public Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(ByEmail.GetValueOrDefault(email));

        public Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(ById.GetValueOrDefault(userId));

        public Task UpdateLastLoginAtAsync(Guid userId, DateTimeOffset loginAt, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class PlaintextPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => password;
        public bool VerifyPassword(string hashedPassword, string password) =>
            string.Equals(hashedPassword, password, StringComparison.Ordinal);
    }
}

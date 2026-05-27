using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Application.Retrieval;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Api.Tests.Support;
using KnowledgeOps.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeOps.Api.Tests.OperationalSafety;

public sealed class OperationalSafetyApiTests : IClassFixture<OperationalSafetyApiTestFactory>
{
    private readonly OperationalSafetyApiTestFactory _factory;

    public OperationalSafetyApiTests(OperationalSafetyApiTestFactory factory)
    {
        _factory = factory;
        _factory.AuditWriter.Reset();
        _factory.DatabaseHealth.IsHealthy = true;
        _factory.RetrievalHealth.IsHealthy = true;
    }

    [Fact]
    public async Task Health_BasicIsPublicAndDoesNotExposeDependencyDetails()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/health");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"Healthy\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("database", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CorrelationId_PreservesSafeHeaderAndReturnsItInCanonicalError()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/operational/not-found");
        request.Headers.Add("X-Correlation-ID", "request_SAFE-123");

        var response = await _factory.CreateClient().SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("request_SAFE-123", response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal(
            "request_SAFE-123",
            body.GetProperty("error").GetProperty("correlationId").GetString());
    }

    [Theory]
    [InlineData("contains spaces")]
    [InlineData("invalid/value")]
    public async Task CorrelationId_ReplacesUnsafeIncomingHeader(string incoming)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/health");
        request.Headers.Add("X-Correlation-ID", incoming);

        var response = await _factory.CreateClient().SendAsync(request);
        var correlationId = response.Headers.GetValues("X-Correlation-ID").Single();

        Assert.NotEqual(incoming, correlationId);
        Assert.Equal(32, correlationId.Length);
        Assert.True(CorrelationIdPolicy.IsAccepted(correlationId));
    }

    [Fact]
    public async Task CorrelationId_ReplacesIncomingHeaderLongerThanStorageLimit()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/health");
        request.Headers.Add("X-Correlation-ID", new string('a', 101));

        var response = await _factory.CreateClient().SendAsync(request);
        var correlationId = response.Headers.GetValues("X-Correlation-ID").Single();

        Assert.Equal(32, correlationId.Length);
        Assert.True(CorrelationIdPolicy.IsAccepted(correlationId));
    }

    [Theory]
    [InlineData("unauthenticated", HttpStatusCode.Unauthorized, "unauthenticated")]
    [InlineData("forbidden", HttpStatusCode.Forbidden, "forbidden")]
    [InlineData("not-found", HttpStatusCode.NotFound, "not_found")]
    [InlineData("conflict", HttpStatusCode.Conflict, "conflict")]
    [InlineData("unavailable", HttpStatusCode.ServiceUnavailable, "service_unavailable")]
    public async Task ExceptionMapping_ReturnsCanonicalSafeEnvelope(
        string endpoint,
        HttpStatusCode statusCode,
        string code)
    {
        var response = await _factory.CreateClient().GetAsync($"/api/test/operational/{endpoint}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal(code, body.GetProperty("error").GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            body.GetProperty("error").GetProperty("correlationId").GetString()));
        Assert.Equal(JsonValueKind.Array, body.GetProperty("error").GetProperty("details").ValueKind);
    }

    [Fact]
    public async Task ExceptionMapping_ValidationReturnsSanitizedValidationItems()
    {
        var response = await _factory.CreateClient().GetAsync("/api/test/operational/validation");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_error", body.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(
            "name",
            body.GetProperty("error").GetProperty("details")[0].GetProperty("field").GetString());
    }

    [Fact]
    public async Task ModelValidation_ReturnsCanonicalResponseWithoutAttemptedValue()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "not-an-email", password = "password-value" });
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("validation_error", text, StringComparison.Ordinal);
        Assert.DoesNotContain("not-an-email", text, StringComparison.Ordinal);
        Assert.DoesNotContain("password-value", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExceptionMapping_UnhandledExceptionDoesNotExposeExceptionContent()
    {
        var response = await _factory.CreateClient().GetAsync("/api/test/operational/unexpected");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("internal_error", text, StringComparison.Ordinal);
        Assert.DoesNotContain("connection string", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthDetails_WithoutAuthenticationReturnsCanonical401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/health/details");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Login_EmitsOnlySafeSuccessAndFailureAuditEvents()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = OperationalSafetyApiTestFactory.AdminEmail, password = OperationalSafetyApiTestFactory.Password });
        await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "unknown@example.test", password = "not-recorded" });

        Assert.Contains(
            _factory.AuditWriter.Events,
            auditEvent => auditEvent.EventType == AuditEventTypes.UserLoginSuccess
                && auditEvent.UserId.HasValue
                && auditEvent.Message == "User login succeeded.");
        Assert.Contains(
            _factory.AuditWriter.Events,
            auditEvent => auditEvent.EventType == AuditEventTypes.UserLoginFailure
                && auditEvent.UserId is null
                && auditEvent.OrganizationId is null
                && auditEvent.Message == "User login failed.");
        Assert.DoesNotContain(
            _factory.AuditWriter.Events,
            auditEvent => auditEvent.Message.Contains("not-recorded", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HealthDetails_AsAgentReturnsCanonical403AndWritesPermissionDeniedAudit()
    {
        var client = await CreateAuthenticatedClientAsync(OperationalSafetyApiTestFactory.AgentEmail);
        _factory.AuditWriter.Reset();

        var response = await client.GetAsync("/api/v1/health/details");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("forbidden", body.GetProperty("error").GetProperty("code").GetString());
        Assert.Contains(
            _factory.AuditWriter.Events,
            auditEvent => auditEvent.EventType == AuditEventTypes.PermissionDenied);
    }

    [Fact]
    public async Task HealthDetails_AsAdminReturnsOnlyApprovedDetailsAndWritesAudit()
    {
        var client = await CreateAuthenticatedClientAsync(OperationalSafetyApiTestFactory.AdminEmail);
        _factory.AuditWriter.Reset();

        var response = await client.GetAsync("/api/v1/health/details");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"application\":\"Healthy\"", text, StringComparison.Ordinal);
        Assert.Contains("\"database\":\"Healthy\"", text, StringComparison.Ordinal);
        Assert.Contains("\"retrieval\":\"Healthy\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("provider", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storage", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("queue", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            _factory.AuditWriter.Events,
            auditEvent => auditEvent.EventType == AuditEventTypes.HealthDetailsViewed);
    }

    [Fact]
    public async Task HealthDetails_DatabaseUnavailableReturnsSanitizedDependencyStatus()
    {
        var client = await CreateAuthenticatedClientAsync(OperationalSafetyApiTestFactory.AdminEmail);
        _factory.DatabaseHealth.IsHealthy = false;

        var response = await client.GetAsync("/api/v1/health/details");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("\"database\":\"Unavailable\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("connection", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthDetails_RetrievalUnavailableReturnsSanitizedDependencyStatus()
    {
        var client = await CreateAuthenticatedClientAsync(OperationalSafetyApiTestFactory.AdminEmail);
        _factory.RetrievalHealth.IsHealthy = false;

        var response = await client.GetAsync("/api/v1/health/details");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("\"retrieval\":\"Unavailable\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("connection", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vector", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuditWriteFailure_DoesNotBreakLoginOrHealthDetailResponses()
    {
        _factory.AuditWriter.ThrowOnWrite = true;
        try
        {
            var client = await CreateAuthenticatedClientAsync(OperationalSafetyApiTestFactory.AdminEmail);
            var healthResponse = await client.GetAsync("/api/v1/health/details");

            Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        }
        finally
        {
            _factory.AuditWriter.ThrowOnWrite = false;
        }
    }

    [Fact]
    public async Task AuditWriteFailure_DoesNotBreakPermissionDenialResponse()
    {
        var client = await CreateAuthenticatedClientAsync(OperationalSafetyApiTestFactory.AgentEmail);
        _factory.AuditWriter.ThrowOnWrite = true;
        try
        {
            var response = await client.GetAsync("/api/v1/health/details");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            _factory.AuditWriter.ThrowOnWrite = false;
        }
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = OperationalSafetyApiTestFactory.Password });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            login.GetProperty("accessToken").GetString());
        return client;
    }
}

public sealed class OperationalSafetyApiTestFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@operational.test";
    public const string AgentEmail = "agent@operational.test";
    public const string Password = "test-password";

    public RecordingAuditEventWriter AuditWriter { get; } = new();
    public StubDatabaseHealthCheck DatabaseHealth { get; } = new();
    public StubRetrievalStorageHealthCheck RetrievalHealth { get; } = new();

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
            services.AddScoped<IUserAuthRepository, FakeUserAuthRepository>();
            services.RemoveAll<IPasswordHasher>();
            services.AddSingleton<IPasswordHasher, PlaintextPasswordHasher>();

            services.RemoveAll<IAuditEventWriter>();
            services.AddSingleton<IAuditEventWriter>(AuditWriter);
            services.RemoveAll<IDatabaseHealthCheck>();
            services.AddSingleton<IDatabaseHealthCheck>(DatabaseHealth);
            services.RemoveAll<IRetrievalStorageHealthCheck>();
            services.AddSingleton<IRetrievalStorageHealthCheck>(RetrievalHealth);
            services.RemoveAll<IUserAccessStateReader>();
            services.AddSingleton(new AccessStateOverrides());
            services.AddScoped<IUserAccessStateReader, RepositoryUserAccessStateReader>();

            services.AddControllers()
                .AddApplicationPart(typeof(OperationalSafetyApiTestFactory).Assembly);
        });
    }

    public sealed class RecordingAuditEventWriter : IAuditEventWriter
    {
        public List<AuditEvent> Events { get; } = [];
        public bool ThrowOnWrite { get; set; }

        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            if (ThrowOnWrite)
                throw new InvalidOperationException("simulated audit failure");

            Events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public void Reset()
        {
            Events.Clear();
            ThrowOnWrite = false;
        }
    }

    public sealed class StubDatabaseHealthCheck : IDatabaseHealthCheck
    {
        public bool IsHealthy { get; set; } = true;

        public Task<DatabaseHealthResult> CheckAsync(CancellationToken ct = default) =>
            Task.FromResult(IsHealthy
                ? DatabaseHealthResult.Healthy
                : DatabaseHealthResult.Unavailable);
    }

    public sealed class StubRetrievalStorageHealthCheck : IRetrievalStorageHealthCheck
    {
        public bool IsHealthy { get; set; } = true;

        public Task<RetrievalStorageHealthResult> CheckAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new RetrievalStorageHealthResult(
                IsHealthy,
                "LocalSqlVectorStore",
                IndexedEmbeddingCount: 0,
                FailedIndexCount: 0,
                IsHealthy ? null : "Retrieval storage is unavailable."));
    }

    private sealed class FakeUserAuthRepository : IUserAuthRepository
    {
        private static readonly Guid OrgId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        private static readonly Dictionary<string, UserAuthRecord> Users =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [AdminEmail] = new(Guid.NewGuid(), OrgId, AdminEmail, "Admin",
                    Password, UserStatus.Active, ["Admin"]),
                [AgentEmail] = new(Guid.NewGuid(), OrgId, AgentEmail, "Agent",
                    Password, UserStatus.Active, ["Agent"])
            };

        public Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(Users.GetValueOrDefault(email));

        public Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Users.Values.SingleOrDefault(user => user.UserId == userId));

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

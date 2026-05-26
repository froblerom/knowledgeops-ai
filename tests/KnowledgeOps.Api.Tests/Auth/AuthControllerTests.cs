using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeOps.Api.Tests.Auth;

public sealed class AuthControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(AuthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = AuthWebApplicationFactory.ActiveEmail, password = AuthWebApplicationFactory.CorrectPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        Assert.Equal(AuthWebApplicationFactory.ActiveEmail,
            body.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "nobody@example.com", password = "password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = AuthWebApplicationFactory.ActiveEmail, password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_DisabledUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = AuthWebApplicationFactory.DisabledEmail, password = AuthWebApplicationFactory.CorrectPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_PendingUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = AuthWebApplicationFactory.PendingEmail, password = AuthWebApplicationFactory.CorrectPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_AllFailures_ReturnIdenticalGenericErrorApartFromCorrelationId()
    {
        var unknown = await (await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "nobody@example.com", password = "p" }))
            .Content.ReadFromJsonAsync<JsonElement>();

        var wrong = await (await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = AuthWebApplicationFactory.ActiveEmail, password = "wrong" }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            unknown.GetProperty("error").GetProperty("code").GetString(),
            wrong.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(
            unknown.GetProperty("error").GetProperty("message").GetString(),
            wrong.GetProperty("error").GetProperty("message").GetString());
        Assert.Equal("Invalid credentials.", unknown.GetProperty("error").GetProperty("message").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            unknown.GetProperty("error").GetProperty("correlationId").GetString()));
    }

    [Fact]
    public async Task Me_WithValidToken_Returns200()
    {
        var token = await GetTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(AuthWebApplicationFactory.ActiveEmail, body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidToken_Returns200()
    {
        var token = await GetTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> GetTokenAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = AuthWebApplicationFactory.ActiveEmail, password = AuthWebApplicationFactory.CorrectPassword });
        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }
}

public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string ActiveEmail = "active@example.com";
    public const string DisabledEmail = "disabled@example.com";
    public const string PendingEmail = "pending@example.com";
    public const string CorrectPassword = "correct-password";

    private static readonly Guid ActiveUserId = Guid.NewGuid();
    private static readonly Guid DisabledUserId = Guid.NewGuid();
    private static readonly Guid PendingUserId = Guid.NewGuid();
    private static readonly Guid OrgId = Guid.NewGuid();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
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
            services.AddScoped<IUserAuthRepository>(_ => new FakeUserAuthRepository());

            services.RemoveAll<IPasswordHasher>();
            services.AddSingleton<IPasswordHasher, PlaintextPasswordHasher>();

            services.RemoveAll<IAuditEventWriter>();
            services.AddSingleton<IAuditEventWriter, NoopAuditEventWriter>();
        });
    }

    private sealed class FakeUserAuthRepository : IUserAuthRepository
    {
        private static readonly Dictionary<string, UserAuthRecord> ByEmail = new()
        {
            [ActiveEmail] = new UserAuthRecord(ActiveUserId, OrgId, ActiveEmail, "Active User",
                CorrectPassword, UserStatus.Active, ["Agent"]),
            [DisabledEmail] = new UserAuthRecord(DisabledUserId, OrgId, DisabledEmail, "Disabled User",
                CorrectPassword, UserStatus.Disabled, ["Agent"]),
            [PendingEmail] = new UserAuthRecord(PendingUserId, OrgId, PendingEmail, "Pending User",
                CorrectPassword, UserStatus.Pending, ["Agent"])
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

    private sealed class NoopAuditEventWriter : IAuditEventWriter
    {
        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}

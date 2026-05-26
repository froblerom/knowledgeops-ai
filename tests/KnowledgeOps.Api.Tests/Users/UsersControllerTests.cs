using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeOps.Api.Tests.Support;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Application.Users;
using KnowledgeOps.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeOps.Api.Tests.Users;

public sealed class UsersControllerTests : IClassFixture<UsersApiTestFactory>
{
    private readonly UsersApiTestFactory _factory;

    public UsersControllerTests(UsersApiTestFactory factory)
    {
        _factory = factory;
        _factory.Reset();
    }

    [Fact]
    public async Task List_WithoutTokenReturns401_AndAgentReturns403()
    {
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _factory.CreateClient().GetAsync("/api/v1/users")).StatusCode);

        var agent = await AuthenticateAsync(UsersApiTestFactory.AgentEmail);
        Assert.Equal(HttpStatusCode.Forbidden, (await agent.GetAsync("/api/v1/users")).StatusCode);
    }

    [Fact]
    public async Task List_AsAdminReturnsOnlyActorOrganizationSafeDtos()
    {
        var response = await (await AuthenticateAsync(UsersApiTestFactory.AdminEmail))
            .GetAsync("/api/v1/users");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.All(body.EnumerateArray(), user =>
            Assert.Equal(UsersApiTestFactory.OrgId, user.GetProperty("organizationId").GetGuid()));
        Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(UsersApiTestFactory.OtherEmail, raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Detail_CrossOrganizationReturnsSafe404()
    {
        var client = await AuthenticateAsync(UsersApiTestFactory.AdminEmail);
        var response = await client.GetAsync($"/api/v1/users/{UsersApiTestFactory.OtherUserId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_NormalizesEmailHashesPasswordAndDoesNotExposeIt()
    {
        var client = await AuthenticateAsync(UsersApiTestFactory.AdminEmail);
        var response = await client.PostAsJsonAsync(
            "/api/v1/users",
            new
            {
                displayName = "New Agent",
                email = "  New.Agent@Example.COM ",
                status = "Active",
                roles = new[] { "Agent" },
                initialPassword = "Do-Not-Return"
            });
        var raw = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("new.agent@example.com", body.GetProperty("email").GetString());
        Assert.Equal("hash:Do-Not-Return", _factory.Repository.LastPasswordHash);
        Assert.DoesNotContain("Do-Not-Return", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("hash:", raw, StringComparison.Ordinal);
        Assert.Contains(_factory.Audit.Events, e => e.EventType == AuditEventTypes.UserCreated);
    }

    [Fact]
    public async Task Create_DuplicateNormalizedEmailReturnsSafe409()
    {
        var response = await (await AuthenticateAsync(UsersApiTestFactory.AdminEmail)).PostAsJsonAsync(
            "/api/v1/users",
            new
            {
                displayName = "Duplicate",
                email = " ADMIN@EXAMPLE.TEST ",
                status = "Active",
                roles = Array.Empty<string>(),
                initialPassword = "pw"
            });
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.DoesNotContain(UsersApiTestFactory.AdminEmail, raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidStatusRoleAndOrganizationMutationReturnSafeErrors()
    {
        var client = await AuthenticateAsync(UsersApiTestFactory.AdminEmail);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync(
                $"/api/v1/users/{UsersApiTestFactory.TargetUserId}/roles",
                new { roleName = "Owner" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.PutAsJsonAsync(
                $"/api/v1/users/{UsersApiTestFactory.TargetUserId}",
                new { displayName = "Target", email = "target@example.test", status = "Deleted" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync(
                $"/api/v1/users/{UsersApiTestFactory.TargetUserId}",
                new
                {
                    displayName = "Target",
                    email = "target@example.test",
                    organizationId = UsersApiTestFactory.OtherOrgId,
                    status = "Active"
                })).StatusCode);
    }

    [Fact]
    public async Task RoleAssignmentIsIdempotent_AndSelfLockoutIsForbidden()
    {
        var client = await AuthenticateAsync(UsersApiTestFactory.AdminEmail);
        await client.PostAsJsonAsync(
            $"/api/v1/users/{UsersApiTestFactory.TargetUserId}/roles",
            new { roleName = "Supervisor" });
        await client.PostAsJsonAsync(
            $"/api/v1/users/{UsersApiTestFactory.TargetUserId}/roles",
            new { roleName = "Supervisor" });

        Assert.Equal(1, _factory.Repository.AssignedSupervisorCount);
        Assert.Single(_factory.Audit.Events, e => e.EventType == AuditEventTypes.UserRoleAssigned);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.DeleteAsync($"/api/v1/users/{UsersApiTestFactory.AdminUserId}/roles/Admin")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync(
                $"/api/v1/users/{UsersApiTestFactory.AdminUserId}",
                new { displayName = "Admin", email = UsersApiTestFactory.AdminEmail, status = "Disabled" })).StatusCode);
    }

    private async Task<HttpClient> AuthenticateAsync(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = UsersApiTestFactory.Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.GetProperty("accessToken").GetString());
        return client;
    }
}

public sealed class UsersApiTestFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@example.test";
    public const string AgentEmail = "agent@example.test";
    public const string OtherEmail = "other@example.test";
    public const string Password = "test-password";
    public static readonly Guid OrgId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid OtherOrgId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    public static readonly Guid AgentUserId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    public static readonly Guid TargetUserId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    public static readonly Guid OtherUserId = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");

    public FakeUserManagementRepository Repository { get; } = new();
    public RecordingAuditWriter Audit { get; } = new();

    public void Reset()
    {
        Repository.Reset();
        Audit.Events.Clear();
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
            services.AddSingleton(new AccessStateOverrides());
            services.AddScoped<IUserAccessStateReader, RepositoryUserAccessStateReader>();
            services.RemoveAll<IUserManagementRepository>();
            services.AddSingleton<IUserManagementRepository>(Repository);
            services.RemoveAll<IPasswordHasher>();
            services.AddSingleton<IPasswordHasher, TestPasswordHasher>();
            services.RemoveAll<IAuditEventWriter>();
            services.AddSingleton<IAuditEventWriter>(Audit);
        });
    }

    public sealed class FakeUserManagementRepository : IUserManagementRepository
    {
        private readonly List<ManagedUser> _users = [];
        public string? LastPasswordHash { get; private set; }
        public int AssignedSupervisorCount { get; private set; }

        public void Reset()
        {
            _users.Clear();
            var now = DateTimeOffset.UtcNow;
            _users.Add(new ManagedUser(AdminUserId, "Admin", AdminEmail, OrgId, UserStatus.Active, ["Admin"], now, now, null));
            _users.Add(new ManagedUser(TargetUserId, "Target", "target@example.test", OrgId, UserStatus.Active, ["Agent"], now, now, null));
            _users.Add(new ManagedUser(OtherUserId, "Other", OtherEmail, OtherOrgId, UserStatus.Active, ["Admin"], now, now, null));
            LastPasswordHash = null;
            AssignedSupervisorCount = 0;
        }

        public Task<IReadOnlyList<ManagedUser>> ListAsync(Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ManagedUser>>(_users.Where(u => u.OrganizationId == organizationId).ToArray());

        public Task<ManagedUser?> FindAsync(Guid userId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(_users.SingleOrDefault(u => u.UserId == userId && u.OrganizationId == organizationId));

        public Task<ManagedUser> CreateAsync(NewManagedUser user, CancellationToken ct = default)
        {
            LastPasswordHash = user.PasswordHash;
            if (_users.Any(existing => existing.Email == user.Email))
                throw new ApplicationConflictException();
            var created = new ManagedUser(
                user.UserId, user.DisplayName, user.Email, user.OrganizationId, user.Status,
                user.Roles.Select(role => role.ToString()).ToArray(), user.CreatedAt, user.CreatedAt, null);
            _users.Add(created);
            return Task.FromResult(created);
        }

        public Task<UserWriteResult> UpdateAsync(Guid userId, Guid organizationId, ManagedUserUpdate update, CancellationToken ct = default) =>
            Task.FromResult(new UserWriteResult(UserWriteOutcome.Applied, FindExisting(userId, organizationId), true));

        public Task<UserWriteResult> AddRoleAsync(Guid userId, Guid organizationId, UserRoleName role, Guid assignedByUserId, DateTimeOffset assignedAt, CancellationToken ct = default)
        {
            var existing = FindExisting(userId, organizationId);
            if (existing is null)
                return Task.FromResult(new UserWriteResult(UserWriteOutcome.NotFound));
            if (existing.Roles.Contains(role.ToString(), StringComparer.Ordinal))
                return Task.FromResult(new UserWriteResult(UserWriteOutcome.NoChange, existing));
            if (role == UserRoleName.Supervisor)
                AssignedSupervisorCount++;
            var updated = existing with { Roles = existing.Roles.Append(role.ToString()).ToArray() };
            _users[_users.IndexOf(existing)] = updated;
            return Task.FromResult(new UserWriteResult(UserWriteOutcome.Applied, updated));
        }

        public Task<UserWriteResult> RemoveRoleAsync(Guid userId, Guid organizationId, UserRoleName role, CancellationToken ct = default) =>
            Task.FromResult(new UserWriteResult(UserWriteOutcome.Applied, FindExisting(userId, organizationId)));

        private ManagedUser? FindExisting(Guid userId, Guid organizationId) =>
            _users.SingleOrDefault(user => user.UserId == userId && user.OrganizationId == organizationId);
    }

    private sealed class FakeAuthRepository : IUserAuthRepository
    {
        private static readonly Dictionary<string, UserAuthRecord> Users =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [AdminEmail] = new(AdminUserId, OrgId, AdminEmail, "Admin", Password, UserStatus.Active, ["Admin"]),
                [AgentEmail] = new(AgentUserId, OrgId, AgentEmail, "Agent", Password, UserStatus.Active, ["Agent"])
            };

        public Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(Users.GetValueOrDefault(email));
        public Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Users.Values.SingleOrDefault(user => user.UserId == userId));
        public Task UpdateLastLoginAtAsync(Guid userId, DateTimeOffset loginAt, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hash:{password}";
        public bool VerifyPassword(string hashedPassword, string password) => hashedPassword == password;
    }

    public sealed class RecordingAuditWriter : IAuditEventWriter
    {
        public List<AuditEvent> Events { get; } = [];
        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}

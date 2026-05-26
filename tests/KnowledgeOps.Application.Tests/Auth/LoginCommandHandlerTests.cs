using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Auth.Commands;
using KnowledgeOps.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Auth;

public sealed class LoginCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OrgId = Guid.NewGuid();
    private const string HashedPassword = "$2a$12$hashedpassword";

    private static UserAuthRecord ActiveUser(string? passwordHash = HashedPassword) =>
        new(UserId, OrgId, "agent@example.com", "Agent One", passwordHash, UserStatus.Active, ["Agent"]);

    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsLoginResult()
    {
        var repo = new StubUserAuthRepository(ActiveUser());
        var hasher = new StubPasswordHasher(verifyResult: true);
        var tokenService = new StubTokenService();
        var handler = BuildHandler(repo, hasher, tokenService);

        var result = await handler.HandleAsync(new LoginCommand("agent@example.com", "password"));

        Assert.NotNull(result);
        Assert.Equal(UserId, result.UserId);
        Assert.Equal(OrgId, result.OrganizationId);
        Assert.Equal("agent@example.com", result.Email);
        Assert.Equal("stub-token", result.AccessToken);
    }

    [Fact]
    public async Task HandleAsync_NormalizesEmailBeforeLookup()
    {
        var repo = new StubUserAuthRepository(ActiveUser());
        var handler = BuildHandler(repo, new StubPasswordHasher(verifyResult: true), new StubTokenService());

        await handler.HandleAsync(new LoginCommand("  Agent@Example.Com ", "password"));

        Assert.Equal("agent@example.com", repo.LastLookupEmail);
    }

    [Fact]
    public async Task HandleAsync_ValidLogin_UpdatesLastLoginAt()
    {
        var repo = new StubUserAuthRepository(ActiveUser());
        var handler = BuildHandler(repo, new StubPasswordHasher(verifyResult: true), new StubTokenService());

        await handler.HandleAsync(new LoginCommand("agent@example.com", "password"));

        Assert.NotNull(repo.LastLoginAtUpdatedFor);
        Assert.Equal(UserId, repo.LastLoginAtUpdatedFor.Value);
    }

    [Fact]
    public async Task HandleAsync_UnknownEmail_ReturnsNull()
    {
        var repo = new StubUserAuthRepository(user: null);
        var handler = BuildHandler(repo, new StubPasswordHasher(verifyResult: false), new StubTokenService());

        var result = await handler.HandleAsync(new LoginCommand("unknown@example.com", "password"));

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ReturnsNull()
    {
        var repo = new StubUserAuthRepository(ActiveUser());
        var handler = BuildHandler(repo, new StubPasswordHasher(verifyResult: false), new StubTokenService());

        var result = await handler.HandleAsync(new LoginCommand("agent@example.com", "wrong"));

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_DisabledUser_ReturnsNull()
    {
        var user = new UserAuthRecord(UserId, OrgId, "agent@example.com", "Agent One",
            HashedPassword, UserStatus.Disabled, ["Agent"]);
        var repo = new StubUserAuthRepository(user);
        var handler = BuildHandler(repo, new StubPasswordHasher(verifyResult: true), new StubTokenService());

        var result = await handler.HandleAsync(new LoginCommand("agent@example.com", "password"));

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_PendingUser_ReturnsNull()
    {
        var user = new UserAuthRecord(UserId, OrgId, "agent@example.com", "Agent One",
            HashedPassword, UserStatus.Pending, ["Agent"]);
        var repo = new StubUserAuthRepository(user);
        var handler = BuildHandler(repo, new StubPasswordHasher(verifyResult: true), new StubTokenService());

        var result = await handler.HandleAsync(new LoginCommand("agent@example.com", "password"));

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_NullPasswordHash_ReturnsNull()
    {
        var repo = new StubUserAuthRepository(ActiveUser(passwordHash: null));
        var handler = BuildHandler(repo, new StubPasswordHasher(verifyResult: true), new StubTokenService());

        var result = await handler.HandleAsync(new LoginCommand("agent@example.com", "password"));

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_FailedLogin_DoesNotUpdateLastLoginAt()
    {
        var repo = new StubUserAuthRepository(user: null);
        var handler = BuildHandler(repo, new StubPasswordHasher(verifyResult: false), new StubTokenService());

        await handler.HandleAsync(new LoginCommand("unknown@example.com", "password"));

        Assert.Null(repo.LastLoginAtUpdatedFor);
    }

    private static LoginCommandHandler BuildHandler(
        IUserAuthRepository repo,
        IPasswordHasher hasher,
        ITokenService tokenService) =>
        new(repo, hasher, tokenService, NullLogger<LoginCommandHandler>.Instance);

    private sealed class StubUserAuthRepository(UserAuthRecord? user) : IUserAuthRepository
    {
        public Guid? LastLoginAtUpdatedFor { get; private set; }
        public string? LastLookupEmail { get; private set; }

        public Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default)
        {
            LastLookupEmail = email;
            return Task.FromResult(user);
        }

        public Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(user);

        public Task UpdateLastLoginAtAsync(Guid userId, DateTimeOffset loginAt, CancellationToken ct = default)
        {
            LastLoginAtUpdatedFor = userId;
            return Task.CompletedTask;
        }
    }

    private sealed class StubPasswordHasher(bool verifyResult) : IPasswordHasher
    {
        public string HashPassword(string password) => "$2a$12$stub";
        public bool VerifyPassword(string hashedPassword, string password) => verifyResult;
    }

    private sealed class StubTokenService : ITokenService
    {
        public TokenResult IssueToken(Guid userId, Guid organizationId, string email,
            string displayName, IReadOnlyList<string> roles) =>
            new("stub-token", DateTimeOffset.UtcNow.AddHours(1));
    }
}

using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Auth.Queries;
using KnowledgeOps.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Auth;

public sealed class GetCurrentUserQueryHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OrgId = Guid.NewGuid();

    private static UserAuthRecord ActiveUser() =>
        new(UserId, OrgId, "agent@example.com", "Agent One", null, UserStatus.Active, ["Agent"]);

    [Fact]
    public async Task HandleAsync_ActiveUser_ReturnsMappedResult()
    {
        var handler = BuildHandler(ActiveUser());

        var result = await handler.HandleAsync(new GetCurrentUserQuery(UserId));

        Assert.NotNull(result);
        Assert.Equal(UserId, result.UserId);
        Assert.Equal(OrgId, result.OrganizationId);
        Assert.Equal("agent@example.com", result.Email);
        Assert.Equal("Agent One", result.DisplayName);
        Assert.Equal(["Agent"], result.Roles);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ReturnsNull()
    {
        var handler = BuildHandler(user: null);

        var result = await handler.HandleAsync(new GetCurrentUserQuery(UserId));

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_DisabledUser_ReturnsNull()
    {
        var user = new UserAuthRecord(UserId, OrgId, "agent@example.com", "Agent One",
            null, UserStatus.Disabled, ["Agent"]);
        var handler = BuildHandler(user);

        var result = await handler.HandleAsync(new GetCurrentUserQuery(UserId));

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_PendingUser_ReturnsNull()
    {
        var user = new UserAuthRecord(UserId, OrgId, "agent@example.com", "Agent One",
            null, UserStatus.Pending, ["Agent"]);
        var handler = BuildHandler(user);

        var result = await handler.HandleAsync(new GetCurrentUserQuery(UserId));

        Assert.Null(result);
    }

    private static GetCurrentUserQueryHandler BuildHandler(UserAuthRecord? user) =>
        new(new StubUserAuthRepository(user), NullLogger<GetCurrentUserQueryHandler>.Instance);

    private sealed class StubUserAuthRepository(UserAuthRecord? user) : IUserAuthRepository
    {
        public Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(user);

        public Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(user);

        public Task UpdateLastLoginAtAsync(Guid userId, DateTimeOffset loginAt, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}

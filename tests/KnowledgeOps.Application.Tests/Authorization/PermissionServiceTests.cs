using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;

namespace KnowledgeOps.Application.Tests.Authorization;

public sealed class PermissionServiceTests
{
    private static readonly IPermissionService Service = new PermissionService();

    [Fact]
    public void HasPermission_UnauthenticatedUser_ReturnsFalse()
    {
        var user = new FakeCurrentUser(isAuthenticated: false, roles: ["Admin"]);
        Assert.False(Service.HasPermission(user, KnowledgeOpsPermissions.Auth.Login));
    }

    [Fact]
    public void HasPermission_NoRoles_ReturnsFalse()
    {
        var user = new FakeCurrentUser(isAuthenticated: true, roles: []);
        Assert.False(Service.HasPermission(user, KnowledgeOpsPermissions.Auth.Login));
    }

    [Fact]
    public void HasPermission_AgentWithAllowedPermission_ReturnsTrue()
    {
        var user = new FakeCurrentUser(isAuthenticated: true, roles: ["Agent"]);
        Assert.True(Service.HasPermission(user, KnowledgeOpsPermissions.Chat.AskQuestion));
    }

    [Fact]
    public void HasPermission_AgentWithForbiddenPermission_ReturnsFalse()
    {
        var user = new FakeCurrentUser(isAuthenticated: true, roles: ["Agent"]);
        Assert.False(Service.HasPermission(user, KnowledgeOpsPermissions.Documents.Upload));
    }

    [Fact]
    public void HasPermission_KnowledgeAdminWithDocumentsUpload_ReturnsTrue()
    {
        var user = new FakeCurrentUser(isAuthenticated: true, roles: ["KnowledgeAdmin"]);
        Assert.True(Service.HasPermission(user, KnowledgeOpsPermissions.Documents.Upload));
    }

    [Fact]
    public void HasPermission_AdminWithUsersView_ReturnsTrue()
    {
        var user = new FakeCurrentUser(isAuthenticated: true, roles: ["Admin"]);
        Assert.True(Service.HasPermission(user, KnowledgeOpsPermissions.Users.View));
    }

    [Fact]
    public void HasPermission_PersistedAccessStateUsesCurrentRoles()
    {
        var state = new UserAccessState(Guid.NewGuid(), Guid.NewGuid(), ["Agent"]);
        Assert.False(Service.HasPermission(state, KnowledgeOpsPermissions.Users.View));
    }

    [Fact]
    public void HasPermission_NonAdminWithUsersView_ReturnsFalse()
    {
        foreach (var role in new[] { "Agent", "Supervisor", "KnowledgeAdmin", "Manager" })
        {
            var user = new FakeCurrentUser(isAuthenticated: true, roles: [role]);
            Assert.False(
                Service.HasPermission(user, KnowledgeOpsPermissions.Users.View),
                $"Role {role} should not have Users.View");
        }
    }

    [Fact]
    public void HasPermission_UnknownRole_ReturnsFalse()
    {
        var user = new FakeCurrentUser(isAuthenticated: true, roles: ["UnknownRole"]);
        Assert.False(Service.HasPermission(user, KnowledgeOpsPermissions.Auth.Login));
    }

    [Fact]
    public void HasPermission_UnknownPermission_ReturnsFalse()
    {
        var user = new FakeCurrentUser(isAuthenticated: true, roles: ["Admin"]);
        Assert.False(Service.HasPermission(user, "Unknown.Permission"));
    }

    private sealed class FakeCurrentUser(bool isAuthenticated, string[] roles) : ICurrentUser
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid OrganizationId { get; } = Guid.NewGuid();
        public string Email => "test@example.com";
        public string DisplayName => "Test User";
        public IReadOnlyList<string> Roles { get; } = roles;
        public bool IsAuthenticated { get; } = isAuthenticated;
    }
}

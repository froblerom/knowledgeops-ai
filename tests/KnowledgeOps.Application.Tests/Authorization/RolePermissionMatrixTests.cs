using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Domain.Users;

namespace KnowledgeOps.Application.Tests.Authorization;

public sealed class RolePermissionMatrixTests
{
    // ── Agent ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(KnowledgeOpsPermissions.Auth.Login)]
    [InlineData(KnowledgeOpsPermissions.Auth.ViewCurrentUser)]
    [InlineData(KnowledgeOpsPermissions.Chat.AskQuestion)]
    [InlineData(KnowledgeOpsPermissions.Chat.ViewOwnHistory)]
    [InlineData(KnowledgeOpsPermissions.Chat.ViewInteraction)]
    [InlineData(KnowledgeOpsPermissions.Chat.ViewCitations)]
    [InlineData(KnowledgeOpsPermissions.Feedback.Submit)]
    [InlineData(KnowledgeOpsPermissions.Feedback.UpdateOwn)]
    [InlineData(KnowledgeOpsPermissions.System.ViewBasicHealth)]
    public void Agent_HasExpectedPermissions(string permission)
    {
        Assert.True(RolePermissionMatrix.HasPermission([nameof(UserRoleName.Agent)], permission));
    }

    [Theory]
    [InlineData(KnowledgeOpsPermissions.Users.View)]
    [InlineData(KnowledgeOpsPermissions.Users.Create)]
    [InlineData(KnowledgeOpsPermissions.Documents.Upload)]
    [InlineData(KnowledgeOpsPermissions.Documents.View)]
    [InlineData(KnowledgeOpsPermissions.Documents.Disable)]
    [InlineData(KnowledgeOpsPermissions.Chat.ViewScopedHistory)]
    [InlineData(KnowledgeOpsPermissions.Feedback.ViewReviewData)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewOverview)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewDocuments)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewChat)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewFeedback)]
    [InlineData(KnowledgeOpsPermissions.System.ViewHealthDetails)]
    [InlineData(KnowledgeOpsPermissions.System.ViewProcessingFailures)]
    [InlineData(KnowledgeOpsPermissions.Audit.View)]
    public void Agent_DoesNotHaveRestrictedPermissions(string permission)
    {
        Assert.False(RolePermissionMatrix.HasPermission([nameof(UserRoleName.Agent)], permission));
    }

    // ── Supervisor ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(KnowledgeOpsPermissions.Auth.Login)]
    [InlineData(KnowledgeOpsPermissions.Chat.ViewScopedHistory)]
    [InlineData(KnowledgeOpsPermissions.Feedback.ViewReviewData)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewFeedback)]
    [InlineData(KnowledgeOpsPermissions.System.ViewBasicHealth)]
    public void Supervisor_HasExpectedPermissions(string permission)
    {
        Assert.True(RolePermissionMatrix.HasPermission([nameof(UserRoleName.Supervisor)], permission));
    }

    [Theory]
    [InlineData(KnowledgeOpsPermissions.Users.View)]
    [InlineData(KnowledgeOpsPermissions.Documents.Upload)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewOverview)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewDocuments)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewChat)]
    [InlineData(KnowledgeOpsPermissions.System.ViewHealthDetails)]
    [InlineData(KnowledgeOpsPermissions.Audit.View)]
    public void Supervisor_DoesNotHaveRestrictedPermissions(string permission)
    {
        Assert.False(RolePermissionMatrix.HasPermission([nameof(UserRoleName.Supervisor)], permission));
    }

    // ── KnowledgeAdmin ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(KnowledgeOpsPermissions.Documents.Upload)]
    [InlineData(KnowledgeOpsPermissions.Documents.View)]
    [InlineData(KnowledgeOpsPermissions.Documents.ViewProcessingStatus)]
    [InlineData(KnowledgeOpsPermissions.Documents.ViewChunks)]
    [InlineData(KnowledgeOpsPermissions.Documents.Disable)]
    [InlineData(KnowledgeOpsPermissions.Documents.ViewUsage)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewOverview)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewDocuments)]
    [InlineData(KnowledgeOpsPermissions.System.ViewProcessingFailures)]
    public void KnowledgeAdmin_HasExpectedPermissions(string permission)
    {
        Assert.True(RolePermissionMatrix.HasPermission([nameof(UserRoleName.KnowledgeAdmin)], permission));
    }

    [Theory]
    [InlineData(KnowledgeOpsPermissions.Users.View)]
    [InlineData(KnowledgeOpsPermissions.Chat.ViewScopedHistory)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewChat)]
    [InlineData(KnowledgeOpsPermissions.System.ViewHealthDetails)]
    [InlineData(KnowledgeOpsPermissions.Audit.View)]
    public void KnowledgeAdmin_DoesNotHaveRestrictedPermissions(string permission)
    {
        Assert.False(RolePermissionMatrix.HasPermission([nameof(UserRoleName.KnowledgeAdmin)], permission));
    }

    // ── Manager ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewOverview)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewDocuments)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewChat)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewFeedback)]
    [InlineData(KnowledgeOpsPermissions.Chat.ViewScopedHistory)]
    [InlineData(KnowledgeOpsPermissions.Feedback.ViewReviewData)]
    [InlineData(KnowledgeOpsPermissions.Documents.View)]
    public void Manager_HasExpectedPermissions(string permission)
    {
        Assert.True(RolePermissionMatrix.HasPermission([nameof(UserRoleName.Manager)], permission));
    }

    [Theory]
    [InlineData(KnowledgeOpsPermissions.Users.View)]
    [InlineData(KnowledgeOpsPermissions.Documents.Upload)]
    [InlineData(KnowledgeOpsPermissions.Documents.ViewChunks)]
    [InlineData(KnowledgeOpsPermissions.Documents.Disable)]
    [InlineData(KnowledgeOpsPermissions.System.ViewHealthDetails)]
    [InlineData(KnowledgeOpsPermissions.System.ViewProcessingFailures)]
    [InlineData(KnowledgeOpsPermissions.Audit.View)]
    public void Manager_DoesNotHaveRestrictedPermissions(string permission)
    {
        Assert.False(RolePermissionMatrix.HasPermission([nameof(UserRoleName.Manager)], permission));
    }

    // ── Admin ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Admin_HasAllMvpPermissions()
    {
        foreach (var permission in KnowledgeOpsPermissions.AllMvpPermissions)
        {
            Assert.True(
                RolePermissionMatrix.HasPermission([nameof(UserRoleName.Admin)], permission),
                $"Admin should have permission: {permission}");
        }
    }

    // ── Deny-by-default ────────────────────────────────────────────────────────

    [Fact]
    public void UnknownRole_DeniesAllPermissions()
    {
        foreach (var permission in KnowledgeOpsPermissions.AllMvpPermissions)
        {
            Assert.False(
                RolePermissionMatrix.HasPermission(["UnknownRole"], permission),
                $"Unknown role should not have permission: {permission}");
        }
    }

    [Fact]
    public void EmptyRoleList_DeniesAllPermissions()
    {
        foreach (var permission in KnowledgeOpsPermissions.AllMvpPermissions)
        {
            Assert.False(
                RolePermissionMatrix.HasPermission([], permission),
                $"Empty roles should not have permission: {permission}");
        }
    }

    [Fact]
    public void UnknownPermission_DeniesForAllRoles()
    {
        var roles = new[]
        {
            nameof(UserRoleName.Agent),
            nameof(UserRoleName.Supervisor),
            nameof(UserRoleName.KnowledgeAdmin),
            nameof(UserRoleName.Manager),
            nameof(UserRoleName.Admin),
        };

        foreach (var role in roles)
        {
            Assert.False(
                RolePermissionMatrix.HasPermission([role], "Unknown.Permission"),
                $"Role {role} should not have unknown permission");
        }
    }

    // ── GetPermissionsForRole ──────────────────────────────────────────────────

    [Fact]
    public void GetPermissionsForRole_UnknownRole_ReturnsEmptySet()
    {
        var result = RolePermissionMatrix.GetPermissionsForRole("NotARole");
        Assert.Empty(result);
    }

    [Fact]
    public void GetPermissionsForRole_Admin_ReturnsAllMvpPermissions()
    {
        var result = RolePermissionMatrix.GetPermissionsForRole(nameof(UserRoleName.Admin));
        Assert.Equal(KnowledgeOpsPermissions.AllMvpPermissions.Count, result.Count);
    }

    // ── Matrix completeness ────────────────────────────────────────────────────

    [Fact]
    public void Matrix_ContainsAllFiveMvpRoles()
    {
        var matrix = RolePermissionMatrix.GetFullMatrix();
        Assert.Contains(nameof(UserRoleName.Agent), matrix.Keys);
        Assert.Contains(nameof(UserRoleName.Supervisor), matrix.Keys);
        Assert.Contains(nameof(UserRoleName.KnowledgeAdmin), matrix.Keys);
        Assert.Contains(nameof(UserRoleName.Manager), matrix.Keys);
        Assert.Contains(nameof(UserRoleName.Admin), matrix.Keys);
    }

    [Fact]
    public void Matrix_DoesNotContainNonMvpRoles()
    {
        var matrix = RolePermissionMatrix.GetFullMatrix();
        Assert.DoesNotContain("Viewer", matrix.Keys);
        Assert.DoesNotContain("Trainer", matrix.Keys);
        Assert.DoesNotContain("QualityAnalyst", matrix.Keys);
        Assert.DoesNotContain("SuperAdmin", matrix.Keys);
        Assert.DoesNotContain("Owner", matrix.Keys);
    }
}

using KnowledgeOps.Application.Authorization;

namespace KnowledgeOps.Application.Tests.Authorization;

public sealed class KnowledgeOpsPermissionsTests
{
    [Fact]
    public void AllMvpPermissions_ContainsExpected30Permissions()
    {
        Assert.Equal(30, KnowledgeOpsPermissions.AllMvpPermissions.Count);
    }

    [Theory]
    [InlineData(KnowledgeOpsPermissions.Auth.Login)]
    [InlineData(KnowledgeOpsPermissions.Auth.ViewCurrentUser)]
    [InlineData(KnowledgeOpsPermissions.Users.View)]
    [InlineData(KnowledgeOpsPermissions.Users.Create)]
    [InlineData(KnowledgeOpsPermissions.Users.Update)]
    [InlineData(KnowledgeOpsPermissions.Users.Disable)]
    [InlineData(KnowledgeOpsPermissions.Users.AssignRole)]
    [InlineData(KnowledgeOpsPermissions.Users.RemoveRole)]
    [InlineData(KnowledgeOpsPermissions.Documents.Upload)]
    [InlineData(KnowledgeOpsPermissions.Documents.View)]
    [InlineData(KnowledgeOpsPermissions.Documents.ViewProcessingStatus)]
    [InlineData(KnowledgeOpsPermissions.Documents.ViewChunks)]
    [InlineData(KnowledgeOpsPermissions.Documents.Disable)]
    [InlineData(KnowledgeOpsPermissions.Documents.ViewUsage)]
    [InlineData(KnowledgeOpsPermissions.Chat.AskQuestion)]
    [InlineData(KnowledgeOpsPermissions.Chat.ViewOwnHistory)]
    [InlineData(KnowledgeOpsPermissions.Chat.ViewScopedHistory)]
    [InlineData(KnowledgeOpsPermissions.Chat.ViewInteraction)]
    [InlineData(KnowledgeOpsPermissions.Chat.ViewCitations)]
    [InlineData(KnowledgeOpsPermissions.Feedback.Submit)]
    [InlineData(KnowledgeOpsPermissions.Feedback.UpdateOwn)]
    [InlineData(KnowledgeOpsPermissions.Feedback.ViewReviewData)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewOverview)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewDocuments)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewChat)]
    [InlineData(KnowledgeOpsPermissions.Dashboard.ViewFeedback)]
    [InlineData(KnowledgeOpsPermissions.System.ViewBasicHealth)]
    [InlineData(KnowledgeOpsPermissions.System.ViewHealthDetails)]
    [InlineData(KnowledgeOpsPermissions.System.ViewProcessingFailures)]
    [InlineData(KnowledgeOpsPermissions.Audit.View)]
    public void AllMvpPermissions_ContainsPermission(string permission)
    {
        Assert.Contains(permission, KnowledgeOpsPermissions.AllMvpPermissions);
    }

    [Theory]
    [InlineData("Documents.Enable")]
    [InlineData("Documents.RetryProcessing")]
    [InlineData("KnowledgeGaps.View")]
    [InlineData("KnowledgeGaps.Review")]
    [InlineData("KnowledgeGaps.Resolve")]
    public void AllMvpPermissions_DoesNotContainDeferredPermissions(string deferredPermission)
    {
        Assert.DoesNotContain(deferredPermission, KnowledgeOpsPermissions.AllMvpPermissions);
    }

    [Fact]
    public void AllPermissionNames_UseResourceActionFormat()
    {
        foreach (var permission in KnowledgeOpsPermissions.AllMvpPermissions)
        {
            Assert.Contains('.', permission);
            var parts = permission.Split('.');
            Assert.Equal(2, parts.Length);
            Assert.False(string.IsNullOrWhiteSpace(parts[0]));
            Assert.False(string.IsNullOrWhiteSpace(parts[1]));
        }
    }
}

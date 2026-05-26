using KnowledgeOps.Domain.Users;

namespace KnowledgeOps.Application.Authorization;

// Centralized role-to-permission mapping. Application owns this; API and Angular must not duplicate it.
// Derived from docs/16-security-and-permissions.md Section 6.1.
// Chat.ViewInteraction and Chat.ViewCitations carry a scope convention:
//   - Agent: own-only (enforced by future chat query filters in Sprint 17+)
//   - Supervisor/KnowledgeAdmin/Manager/Admin: same-organization scoped
// The permission grant here does not replace scope enforcement in business workflows.
public static class RolePermissionMatrix
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Matrix =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [nameof(UserRoleName.Agent)] = new HashSet<string>(StringComparer.Ordinal)
            {
                KnowledgeOpsPermissions.Auth.Login,
                KnowledgeOpsPermissions.Auth.ViewCurrentUser,
                KnowledgeOpsPermissions.Chat.AskQuestion,
                KnowledgeOpsPermissions.Chat.ViewOwnHistory,
                KnowledgeOpsPermissions.Chat.ViewInteraction,
                KnowledgeOpsPermissions.Chat.ViewCitations,
                KnowledgeOpsPermissions.Feedback.Submit,
                KnowledgeOpsPermissions.Feedback.UpdateOwn,
                KnowledgeOpsPermissions.System.ViewBasicHealth,
            },

            [nameof(UserRoleName.Supervisor)] = new HashSet<string>(StringComparer.Ordinal)
            {
                KnowledgeOpsPermissions.Auth.Login,
                KnowledgeOpsPermissions.Auth.ViewCurrentUser,
                KnowledgeOpsPermissions.Chat.AskQuestion,
                KnowledgeOpsPermissions.Chat.ViewOwnHistory,
                KnowledgeOpsPermissions.Chat.ViewScopedHistory,
                KnowledgeOpsPermissions.Chat.ViewInteraction,
                KnowledgeOpsPermissions.Chat.ViewCitations,
                KnowledgeOpsPermissions.Feedback.Submit,
                KnowledgeOpsPermissions.Feedback.UpdateOwn,
                KnowledgeOpsPermissions.Feedback.ViewReviewData,
                KnowledgeOpsPermissions.Dashboard.ViewFeedback,
                KnowledgeOpsPermissions.System.ViewBasicHealth,
            },

            [nameof(UserRoleName.KnowledgeAdmin)] = new HashSet<string>(StringComparer.Ordinal)
            {
                KnowledgeOpsPermissions.Auth.Login,
                KnowledgeOpsPermissions.Auth.ViewCurrentUser,
                KnowledgeOpsPermissions.Documents.Upload,
                KnowledgeOpsPermissions.Documents.View,
                KnowledgeOpsPermissions.Documents.ViewProcessingStatus,
                KnowledgeOpsPermissions.Documents.ViewChunks,
                KnowledgeOpsPermissions.Documents.Disable,
                KnowledgeOpsPermissions.Documents.ViewUsage,
                KnowledgeOpsPermissions.Chat.AskQuestion,
                KnowledgeOpsPermissions.Chat.ViewOwnHistory,
                KnowledgeOpsPermissions.Chat.ViewInteraction,
                KnowledgeOpsPermissions.Chat.ViewCitations,
                KnowledgeOpsPermissions.Feedback.Submit,
                KnowledgeOpsPermissions.Feedback.UpdateOwn,
                KnowledgeOpsPermissions.Dashboard.ViewOverview,
                KnowledgeOpsPermissions.Dashboard.ViewDocuments,
                KnowledgeOpsPermissions.System.ViewBasicHealth,
                KnowledgeOpsPermissions.System.ViewProcessingFailures,
            },

            [nameof(UserRoleName.Manager)] = new HashSet<string>(StringComparer.Ordinal)
            {
                KnowledgeOpsPermissions.Auth.Login,
                KnowledgeOpsPermissions.Auth.ViewCurrentUser,
                KnowledgeOpsPermissions.Documents.View,
                KnowledgeOpsPermissions.Documents.ViewProcessingStatus,
                KnowledgeOpsPermissions.Documents.ViewUsage,
                KnowledgeOpsPermissions.Chat.AskQuestion,
                KnowledgeOpsPermissions.Chat.ViewOwnHistory,
                KnowledgeOpsPermissions.Chat.ViewScopedHistory,
                KnowledgeOpsPermissions.Chat.ViewInteraction,
                KnowledgeOpsPermissions.Chat.ViewCitations,
                KnowledgeOpsPermissions.Feedback.Submit,
                KnowledgeOpsPermissions.Feedback.UpdateOwn,
                KnowledgeOpsPermissions.Feedback.ViewReviewData,
                KnowledgeOpsPermissions.Dashboard.ViewOverview,
                KnowledgeOpsPermissions.Dashboard.ViewDocuments,
                KnowledgeOpsPermissions.Dashboard.ViewChat,
                KnowledgeOpsPermissions.Dashboard.ViewFeedback,
                KnowledgeOpsPermissions.System.ViewBasicHealth,
            },

            [nameof(UserRoleName.Admin)] = new HashSet<string>(
                KnowledgeOpsPermissions.AllMvpPermissions,
                StringComparer.Ordinal),
        };

    public static bool HasPermission(IEnumerable<string> roles, string permission)
    {
        foreach (var role in roles)
        {
            if (Matrix.TryGetValue(role, out var permissions) && permissions.Contains(permission))
                return true;
        }
        return false;
    }

    public static IReadOnlySet<string> GetPermissionsForRole(string role) =>
        Matrix.TryGetValue(role, out var permissions)
            ? permissions
            : new HashSet<string>(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, IReadOnlySet<string>> GetFullMatrix() => Matrix;
}

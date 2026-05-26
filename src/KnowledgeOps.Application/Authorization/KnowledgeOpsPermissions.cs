namespace KnowledgeOps.Application.Authorization;

public static class KnowledgeOpsPermissions
{
    public static class Auth
    {
        public const string Login = "Auth.Login";
        public const string ViewCurrentUser = "Auth.ViewCurrentUser";
    }

    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Update = "Users.Update";
        public const string Disable = "Users.Disable";
        public const string AssignRole = "Users.AssignRole";
        public const string RemoveRole = "Users.RemoveRole";
    }

    public static class Documents
    {
        public const string Upload = "Documents.Upload";
        public const string View = "Documents.View";
        public const string ViewProcessingStatus = "Documents.ViewProcessingStatus";
        public const string ViewChunks = "Documents.ViewChunks";
        public const string Disable = "Documents.Disable";
        public const string ViewUsage = "Documents.ViewUsage";
        // Documents.Enable and Documents.RetryProcessing are Phase 2 — not implemented here.
    }

    public static class Chat
    {
        public const string AskQuestion = "Chat.AskQuestion";
        public const string ViewOwnHistory = "Chat.ViewOwnHistory";
        public const string ViewScopedHistory = "Chat.ViewScopedHistory";

        // Convention: ViewInteraction and ViewCitations require future query filters for scope enforcement.
        // Agent = own-only; Supervisor/KnowledgeAdmin/Manager/Admin = same-organization scoped.
        // Sprint 7 establishes the permission grant; chat workflows enforce the boundary in Sprint 17+.
        public const string ViewInteraction = "Chat.ViewInteraction";
        public const string ViewCitations = "Chat.ViewCitations";
    }

    public static class Feedback
    {
        public const string Submit = "Feedback.Submit";
        public const string UpdateOwn = "Feedback.UpdateOwn";
        public const string ViewReviewData = "Feedback.ViewReviewData";
    }

    public static class Dashboard
    {
        public const string ViewOverview = "Dashboard.ViewOverview";
        public const string ViewDocuments = "Dashboard.ViewDocuments";
        public const string ViewChat = "Dashboard.ViewChat";
        public const string ViewFeedback = "Dashboard.ViewFeedback";
    }

    public static class System
    {
        public const string ViewBasicHealth = "System.ViewBasicHealth";
        public const string ViewHealthDetails = "System.ViewHealthDetails";
        public const string ViewProcessingFailures = "System.ViewProcessingFailures";
    }

    public static class Audit
    {
        public const string View = "Audit.View";
    }

    // KnowledgeGaps permissions are Phase 2 — not implemented here.

    public static IReadOnlySet<string> AllMvpPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Auth.Login,
        Auth.ViewCurrentUser,
        Users.View,
        Users.Create,
        Users.Update,
        Users.Disable,
        Users.AssignRole,
        Users.RemoveRole,
        Documents.Upload,
        Documents.View,
        Documents.ViewProcessingStatus,
        Documents.ViewChunks,
        Documents.Disable,
        Documents.ViewUsage,
        Chat.AskQuestion,
        Chat.ViewOwnHistory,
        Chat.ViewScopedHistory,
        Chat.ViewInteraction,
        Chat.ViewCitations,
        Feedback.Submit,
        Feedback.UpdateOwn,
        Feedback.ViewReviewData,
        Dashboard.ViewOverview,
        Dashboard.ViewDocuments,
        Dashboard.ViewChat,
        Dashboard.ViewFeedback,
        System.ViewBasicHealth,
        System.ViewHealthDetails,
        System.ViewProcessingFailures,
        Audit.View,
    };
}

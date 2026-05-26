namespace KnowledgeOps.Application.Observability;

public static class AuditEventTypes
{
    public const string UserLoginSuccess = "UserLoginSuccess";
    public const string UserLoginFailure = "UserLoginFailure";
    public const string PermissionDenied = "PermissionDenied";
    public const string HealthDetailsViewed = "HealthDetailsViewed";
}

namespace KnowledgeOps.Application.Observability;

public static class AuditEventTypes
{
    public const string UserLoginSuccess = "UserLoginSuccess";
    public const string UserLoginFailure = "UserLoginFailure";
    public const string PermissionDenied = "PermissionDenied";
    public const string HealthDetailsViewed = "HealthDetailsViewed";
    public const string UserCreated = "UserCreated";
    public const string UserUpdated = "UserUpdated";
    public const string UserStatusChanged = "UserStatusChanged";
    public const string UserRoleAssigned = "UserRoleAssigned";
    public const string UserRoleRemoved = "UserRoleRemoved";
    public const string UserManagementDenied = "UserManagementDenied";
}

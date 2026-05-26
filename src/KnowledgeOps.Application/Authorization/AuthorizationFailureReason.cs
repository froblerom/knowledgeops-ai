namespace KnowledgeOps.Application.Authorization;

public enum AuthorizationFailureReason
{
    Unauthenticated,
    MissingOrganization,
    MissingTargetOrganization,
    CrossOrganizationAccess,
    MissingPermission,
}

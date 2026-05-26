namespace KnowledgeOps.Application.Authorization;

public sealed class OrganizationScopeResult
{
    private OrganizationScopeResult(bool isAllowed, AuthorizationFailureReason? failureReason)
    {
        IsAllowed = isAllowed;
        FailureReason = failureReason;
    }

    public bool IsAllowed { get; }
    public AuthorizationFailureReason? FailureReason { get; }

    public static OrganizationScopeResult Allowed() =>
        new(isAllowed: true, failureReason: null);

    public static OrganizationScopeResult Denied(AuthorizationFailureReason reason) =>
        new(isAllowed: false, failureReason: reason);
}

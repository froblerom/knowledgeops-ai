using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace KnowledgeOps.Api.Authorization;

// API adapter only. Permission semantics live in IPermissionService (Application layer).
// Logs safe fields only: EventType, UserId, OrganizationId.
// Never logs permission names, role names, tokens, credentials, or resource content.
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IPermissionService permissionService,
        ICurrentUser currentUser,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissionService = permissionService;
        _currentUser = currentUser;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!_currentUser.IsAuthenticated)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        if (_permissionService.HasPermission(_currentUser, requirement.Permission))
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Authorization denied. EventType={EventType} UserId={UserId} OrganizationId={OrganizationId}",
                "PermissionDenied",
                _currentUser.UserId,
                _currentUser.OrganizationId);

            context.Fail();
        }

        return Task.CompletedTask;
    }
}

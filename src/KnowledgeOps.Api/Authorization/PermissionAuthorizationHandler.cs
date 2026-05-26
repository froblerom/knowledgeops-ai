using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Observability;
using Microsoft.AspNetCore.Authorization;

namespace KnowledgeOps.Api.Authorization;

// API adapter only. Permission semantics live in IPermissionService (Application layer).
// Logs safe fields only: EventType, UserId, OrganizationId.
// Never logs permission names, role names, tokens, credentials, or resource content.
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly IUserAccessStateReader _accessStateReader;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditEventWriter _auditEventWriter;
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IPermissionService permissionService,
        IUserAccessStateReader accessStateReader,
        ICurrentUser currentUser,
        IAuditEventWriter auditEventWriter,
        ICorrelationContext correlationContext,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissionService = permissionService;
        _accessStateReader = accessStateReader;
        _currentUser = currentUser;
        _auditEventWriter = auditEventWriter;
        _correlationContext = correlationContext;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!_currentUser.IsAuthenticated)
        {
            context.Fail();
            return;
        }

        var accessState = await _accessStateReader.FindActiveByIdAsync(_currentUser.UserId);
        if (accessState is not null
            && accessState.OrganizationId == _currentUser.OrganizationId
            && _permissionService.HasPermission(accessState, requirement.Permission))
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Authorization denied. EventType={EventType} UserId={UserId} OrganizationId={OrganizationId}",
                AuditEventTypes.PermissionDenied,
                _currentUser.UserId,
                _currentUser.OrganizationId);

            await WriteAuditBestEffortAsync();
            context.Fail();
        }
    }

    private async Task WriteAuditBestEffortAsync()
    {
        try
        {
            await _auditEventWriter.WriteAsync(new AuditEvent(
                AuditEventTypes.PermissionDenied,
                "Permission denied.",
                AuditSeverity.Warning,
                _correlationContext.CorrelationId,
                _currentUser.OrganizationId,
                _currentUser.UserId));
        }
        catch
        {
            _logger.LogWarning(
                "Audit write failed. EventType={EventType} CorrelationId={CorrelationId}",
                AuditEventTypes.PermissionDenied,
                _correlationContext.CorrelationId);
        }
    }
}

using KnowledgeOps.Api.Controllers.Models;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Auth.Commands;
using KnowledgeOps.Application.Auth.Queries;
using KnowledgeOps.Application.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginCommandHandler _loginCommandHandler;
    private readonly GetCurrentUserQueryHandler _getCurrentUserQueryHandler;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditEventWriter _auditEventWriter;
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        LoginCommandHandler loginCommandHandler,
        GetCurrentUserQueryHandler getCurrentUserQueryHandler,
        ICurrentUser currentUser,
        IAuditEventWriter auditEventWriter,
        ICorrelationContext correlationContext,
        ILogger<AuthController> logger)
    {
        _loginCommandHandler = loginCommandHandler;
        _getCurrentUserQueryHandler = getCurrentUserQueryHandler;
        _currentUser = currentUser;
        _auditEventWriter = auditEventWriter;
        _correlationContext = correlationContext;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _loginCommandHandler.HandleAsync(
            new LoginCommand(request.Email, request.Password), ct);

        if (result is null)
        {
            await WriteAuditBestEffortAsync(
                new AuditEvent(
                    AuditEventTypes.UserLoginFailure,
                    "User login failed.",
                    AuditSeverity.Warning,
                    _correlationContext.CorrelationId),
                ct);

            return Unauthorized(ApiErrorResponses.InvalidCredentials(_correlationContext.CorrelationId));
        }

        await WriteAuditBestEffortAsync(
            new AuditEvent(
                AuditEventTypes.UserLoginSuccess,
                "User login succeeded.",
                AuditSeverity.Info,
                _correlationContext.CorrelationId,
                result.OrganizationId,
                result.UserId),
            ct);

        return Ok(new LoginResponse
        {
            AccessToken = result.AccessToken,
            ExpiresAt = result.ExpiresAt,
            User = new CurrentUserResponse
            {
                UserId = result.UserId,
                Email = result.Email,
                DisplayName = result.DisplayName,
                OrganizationId = result.OrganizationId,
                Roles = result.Roles,
                Status = "Active"
            }
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await _getCurrentUserQueryHandler.HandleAsync(
            new GetCurrentUserQuery(_currentUser.UserId), ct);

        if (result is null)
            return Unauthorized(ApiErrorResponses.InvalidCredentials(_correlationContext.CorrelationId));

        return Ok(new CurrentUserResponse
        {
            UserId = result.UserId,
            Email = result.Email,
            DisplayName = result.DisplayName,
            OrganizationId = result.OrganizationId,
            Roles = result.Roles,
            Status = result.Status
        });
    }

    private async Task WriteAuditBestEffortAsync(AuditEvent auditEvent, CancellationToken ct)
    {
        try
        {
            await _auditEventWriter.WriteAsync(auditEvent, ct);
        }
        catch
        {
            _logger.LogWarning(
                "Audit write failed. EventType={EventType} CorrelationId={CorrelationId}",
                auditEvent.EventType,
                auditEvent.CorrelationId);
        }
    }
}

using KnowledgeOps.Api.Authorization;
using KnowledgeOps.Api.Controllers.Models;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Application.Retrieval;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IDatabaseHealthCheck _databaseHealthCheck;
    private readonly IRetrievalStorageHealthCheck _retrievalStorageHealthCheck;
    private readonly IAiProviderDiagnostics _aiProviderDiagnostics;
    private readonly IAuditEventWriter _auditEventWriter;
    private readonly ICorrelationContext _correlationContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IDatabaseHealthCheck databaseHealthCheck,
        IRetrievalStorageHealthCheck retrievalStorageHealthCheck,
        IAiProviderDiagnostics aiProviderDiagnostics,
        IAuditEventWriter auditEventWriter,
        ICorrelationContext correlationContext,
        ICurrentUser currentUser,
        ILogger<HealthController> logger)
    {
        _databaseHealthCheck = databaseHealthCheck;
        _retrievalStorageHealthCheck = retrievalStorageHealthCheck;
        _aiProviderDiagnostics = aiProviderDiagnostics;
        _auditEventWriter = auditEventWriter;
        _correlationContext = correlationContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Basic() =>
        Ok(new BasicHealthResponse("Healthy", DateTimeOffset.UtcNow));

    [HttpGet("details")]
    [RequirePermission(KnowledgeOpsPermissions.System.ViewHealthDetails)]
    public async Task<IActionResult> Details(CancellationToken ct)
    {
        var databaseStatus = await _databaseHealthCheck.CheckAsync(ct);
        var retrievalStatus = await _retrievalStorageHealthCheck.CheckAsync(ct);
        var isHealthy = databaseStatus.IsHealthy && retrievalStatus.IsHealthy;

        await WriteAuditBestEffortAsync(
            new AuditEvent(
                AuditEventTypes.HealthDetailsViewed,
                "Detailed health status viewed.",
                AuditSeverity.Info,
                _correlationContext.CorrelationId,
                _currentUser.OrganizationId,
                _currentUser.UserId),
            ct);

        var response = new DetailedHealthResponse(
            isHealthy ? "Healthy" : "Degraded",
            new HealthDependencyResponse(
                "Healthy",
                databaseStatus.IsHealthy ? "Healthy" : "Unavailable",
                retrievalStatus.IsHealthy ? "Healthy" : "Unavailable"),
            new AiProviderStatusResponse(
                _aiProviderDiagnostics.AnswerProvider,
                _aiProviderDiagnostics.OpenAiConfigured,
                _aiProviderDiagnostics.Model,
                _aiProviderDiagnostics.LocalProviderBaseUrl),
            DateTimeOffset.UtcNow);

        return isHealthy
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
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

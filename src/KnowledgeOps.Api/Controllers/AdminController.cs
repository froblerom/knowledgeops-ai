using KnowledgeOps.Api.Authorization;
using KnowledgeOps.Api.Controllers.Models;
using KnowledgeOps.Application.Admin;
using KnowledgeOps.Application.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
public sealed class AdminController(IAdminSupportService service) : ControllerBase
{
    [HttpGet("processing-failures")]
    [RequirePermission(KnowledgeOpsPermissions.System.ViewProcessingFailures)]
    public async Task<ActionResult<IReadOnlyList<ProcessingFailureResponse>>> GetProcessingFailures(
        [FromQuery] int? limit,
        CancellationToken ct)
    {
        var results = await service.GetProcessingFailuresAsync(limit, ct);

        return Ok(results.Select(item => new ProcessingFailureResponse(
            item.DocumentId,
            item.Title,
            item.ProcessingStatus,
            item.FailureReason,
            item.FailedAt)).ToArray());
    }

    [HttpGet("audit-log")]
    [RequirePermission(KnowledgeOpsPermissions.Audit.View)]
    public async Task<ActionResult<IReadOnlyList<AuditLogEntryResponse>>> GetAuditLog(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? eventType,
        [FromQuery] int? limit,
        CancellationToken ct)
    {
        var results = await service.GetAuditLogAsync(
            new AuditLogQuery(
                ToUtcOffset(from),
                ToUtcOffset(to),
                string.IsNullOrWhiteSpace(eventType) ? null : eventType.Trim(),
                limit),
            ct);

        return Ok(results.Select(item => new AuditLogEntryResponse(
            item.AuditLogEntryId,
            item.EventType,
            item.Message,
            item.Severity,
            item.UserId,
            item.EntityType,
            item.EntityId,
            item.CorrelationId,
            item.CreatedAt)).ToArray());
    }

    private static DateTimeOffset? ToUtcOffset(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        var utc = value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };

        return new DateTimeOffset(utc, TimeSpan.Zero);
    }
}

using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Admin;

internal sealed class AdminSupportService(
    ICurrentUser currentUser,
    IUserAccessStateReader accessStateReader,
    IPermissionService permissionService,
    IDocumentRepository documentRepository,
    IAuditLogRepository auditLogRepository,
    IAuditEventWriter auditEventWriter,
    ICorrelationContext correlationContext,
    ILogger<AdminSupportService> logger) : IAdminSupportService
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 100;

    public async Task<IReadOnlyList<ProcessingFailureResult>> GetProcessingFailuresAsync(
        int? limit,
        CancellationToken ct = default)
    {
        var state = await RequireActiveStateAsync(ct);
        RequirePermission(state, KnowledgeOpsPermissions.System.ViewProcessingFailures);

        var documents = await documentRepository.FindFailedDocumentsAsync(
            state.OrganizationId,
            NormalizeLimit(limit),
            ct);

        return documents
            .Select(doc => new ProcessingFailureResult(
                doc.DocumentId,
                doc.Title,
                doc.ProcessingStatus.ToString(),
                doc.FailureReason,
                doc.UpdatedAt))
            .ToArray();
    }

    public async Task<IReadOnlyList<AuditLogResult>> GetAuditLogAsync(
        AuditLogQuery query,
        CancellationToken ct = default)
    {
        if (query.From.HasValue && query.To.HasValue && query.From > query.To)
            throw new ApplicationValidationException(
                [new ApplicationValidationItem("from", "From must be before or equal to To.")]);

        var state = await RequireActiveStateAsync(ct);
        RequirePermission(state, KnowledgeOpsPermissions.Audit.View);

        var results = await auditLogRepository.FindAsync(
            state.OrganizationId,
            query,
            NormalizeLimit(query.Limit),
            ct);

        await AuditViewedBestEffortAsync(state, query, results.Count, ct);

        return results;
    }

    private async Task<UserAccessState> RequireActiveStateAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            throw new ApplicationUnauthenticatedException();

        var state = await accessStateReader.FindActiveByIdAsync(currentUser.UserId, ct);
        if (state is null)
            throw new ApplicationUnauthenticatedException();

        if (state.OrganizationId == Guid.Empty)
            throw new ApplicationForbiddenException();

        return state;
    }

    private void RequirePermission(UserAccessState state, string permission)
    {
        if (!permissionService.HasPermission(state, permission))
            throw new ApplicationForbiddenException();
    }

    private static int NormalizeLimit(int? limit)
    {
        if (!limit.HasValue || limit.Value <= 0)
            return DefaultLimit;

        return Math.Min(limit.Value, MaxLimit);
    }

    private async Task AuditViewedBestEffortAsync(
        UserAccessState state,
        AuditLogQuery query,
        int count,
        CancellationToken ct)
    {
        try
        {
            await auditEventWriter.WriteAsync(
                new AuditEvent(
                    AuditEventTypes.AuditLogViewed,
                    "Audit log viewed. " +
                    $"Count={count}, EventTypeFilterSet={!string.IsNullOrWhiteSpace(query.EventType)}, " +
                    $"FromSet={query.From.HasValue}, ToSet={query.To.HasValue}.",
                    AuditSeverity.Info,
                    correlationContext.CorrelationId,
                    state.OrganizationId,
                    state.UserId,
                    "AuditLog",
                    null),
                ct);
        }
        catch
        {
            logger.LogWarning(
                "Audit write failed. EventType={EventType} CorrelationId={CorrelationId}",
                AuditEventTypes.AuditLogViewed,
                correlationContext.CorrelationId);
        }
    }
}

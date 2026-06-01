namespace KnowledgeOps.Application.Admin;

public interface IAdminSupportService
{
    Task<IReadOnlyList<ProcessingFailureResult>> GetProcessingFailuresAsync(
        int? limit,
        CancellationToken ct = default);

    Task<IReadOnlyList<AuditLogResult>> GetAuditLogAsync(
        AuditLogQuery query,
        CancellationToken ct = default);
}

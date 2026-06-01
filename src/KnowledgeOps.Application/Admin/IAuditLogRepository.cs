namespace KnowledgeOps.Application.Admin;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLogResult>> FindAsync(
        Guid organizationId,
        AuditLogQuery query,
        int limit,
        CancellationToken ct = default);
}

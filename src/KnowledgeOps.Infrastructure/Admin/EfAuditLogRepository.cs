using KnowledgeOps.Application.Admin;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Admin;

internal sealed class EfAuditLogRepository(KnowledgeOpsDbContext dbContext) : IAuditLogRepository
{
    public async Task<IReadOnlyList<AuditLogResult>> FindAsync(
        Guid organizationId,
        AuditLogQuery query,
        int limit,
        CancellationToken ct = default)
    {
        var auditQuery = dbContext.AuditLogEntries
            .AsNoTracking()
            .Where(entry => entry.OrganizationId == organizationId);

        if (query.From.HasValue)
            auditQuery = auditQuery.Where(entry => entry.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            auditQuery = auditQuery.Where(entry => entry.CreatedAt <= query.To.Value);

        if (!string.IsNullOrWhiteSpace(query.EventType))
            auditQuery = auditQuery.Where(entry => entry.EventType == query.EventType.Trim());

        return await auditQuery
            .OrderByDescending(entry => entry.CreatedAt)
            .ThenByDescending(entry => entry.Id)
            .Take(limit)
            .Select(entry => new AuditLogResult(
                entry.Id,
                entry.EventType,
                entry.Message,
                entry.Severity.ToString(),
                entry.UserId,
                entry.EntityType,
                entry.EntityId,
                entry.CorrelationId,
                entry.CreatedAt))
            .ToArrayAsync(ct);
    }
}

using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Audit;
using KnowledgeOps.Infrastructure.Persistence;

namespace KnowledgeOps.Infrastructure.Observability;

public sealed class EfAuditEventWriter(KnowledgeOpsDbContext dbContext) : IAuditEventWriter
{
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
    {
        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = auditEvent.OrganizationId,
            UserId = auditEvent.UserId,
            EventType = auditEvent.EventType,
            EntityType = auditEvent.EntityType,
            EntityId = auditEvent.EntityId,
            Message = auditEvent.Message,
            Severity = MapSeverity(auditEvent.Severity),
            CorrelationId = CorrelationIdPolicy.AcceptOrCreate(auditEvent.CorrelationId),
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(ct);
    }

    private static KnowledgeOps.Domain.Audit.AuditSeverity MapSeverity(
        KnowledgeOps.Application.Observability.AuditSeverity severity) =>
        severity switch
        {
            KnowledgeOps.Application.Observability.AuditSeverity.Info =>
                KnowledgeOps.Domain.Audit.AuditSeverity.Info,
            KnowledgeOps.Application.Observability.AuditSeverity.Warning =>
                KnowledgeOps.Domain.Audit.AuditSeverity.Warning,
            KnowledgeOps.Application.Observability.AuditSeverity.Error =>
                KnowledgeOps.Domain.Audit.AuditSeverity.Error,
            KnowledgeOps.Application.Observability.AuditSeverity.Critical =>
                KnowledgeOps.Domain.Audit.AuditSeverity.Critical,
            _ => KnowledgeOps.Domain.Audit.AuditSeverity.Error
        };
}

namespace KnowledgeOps.Application.Observability;

public interface IAuditEventWriter
{
    Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default);
}

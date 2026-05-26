namespace KnowledgeOps.Application.Observability;

public sealed record AuditEvent(
    string EventType,
    string Message,
    AuditSeverity Severity,
    string CorrelationId,
    Guid? OrganizationId = null,
    Guid? UserId = null,
    string? EntityType = null,
    Guid? EntityId = null);

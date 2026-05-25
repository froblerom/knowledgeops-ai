namespace KnowledgeOps.Domain.Audit;

public sealed class AuditLogEntry
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? UserId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public string Message { get; set; } = string.Empty;

    public AuditSeverity Severity { get; set; }

    public string? CorrelationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

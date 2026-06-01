namespace KnowledgeOps.Application.Admin;

public sealed record ProcessingFailureResult(
    Guid DocumentId,
    string Title,
    string ProcessingStatus,
    string? FailureReason,
    DateTimeOffset FailedAt);

public sealed record AuditLogQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? EventType,
    int? Limit = null);

public sealed record AuditLogResult(
    Guid AuditLogEntryId,
    string EventType,
    string Message,
    string Severity,
    Guid? UserId,
    string? EntityType,
    Guid? EntityId,
    string? CorrelationId,
    DateTimeOffset CreatedAt);

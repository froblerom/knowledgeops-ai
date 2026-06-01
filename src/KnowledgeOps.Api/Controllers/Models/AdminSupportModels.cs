namespace KnowledgeOps.Api.Controllers.Models;

public sealed record ProcessingFailureResponse(
    Guid DocumentId,
    string Title,
    string ProcessingStatus,
    string? FailureReason,
    DateTimeOffset FailedAt);

public sealed record AuditLogEntryResponse(
    Guid AuditLogEntryId,
    string EventType,
    string Message,
    string Severity,
    Guid? UserId,
    string? EntityType,
    Guid? EntityId,
    string? CorrelationId,
    DateTimeOffset CreatedAt);

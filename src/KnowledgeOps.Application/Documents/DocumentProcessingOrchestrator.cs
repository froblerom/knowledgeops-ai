using KnowledgeOps.Application.Observability;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Documents;

public sealed class DocumentProcessingOrchestrator : IDocumentProcessingOrchestrator
{
    private const int MaxFailureReasonLength = 200;

    private readonly IDocumentRepository _repository;
    private readonly IReadOnlyList<IDocumentProcessingStep> _steps;
    private readonly IDocumentProcessingTransactionFactory _transactionFactory;
    private readonly IAuditEventWriter _auditEventWriter;
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<DocumentProcessingOrchestrator> _logger;

    public DocumentProcessingOrchestrator(
        IDocumentRepository repository,
        IEnumerable<IDocumentProcessingStep> processingSteps,
        IDocumentProcessingTransactionFactory transactionFactory,
        IAuditEventWriter auditEventWriter,
        ICorrelationContext correlationContext,
        ILogger<DocumentProcessingOrchestrator> logger)
    {
        _repository = repository;
        _steps = processingSteps.ToList();
        _transactionFactory = transactionFactory;
        _auditEventWriter = auditEventWriter;
        _correlationContext = correlationContext;
        _logger = logger;
    }

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _repository.FindPendingForProcessingAsync(maxCount: 1, cancellationToken);
        if (pending.Count == 0)
            return false;

        var candidate = pending[0];
        var now = DateTimeOffset.UtcNow;

        var claimed = await _repository.ClaimForProcessingAsync(candidate.DocumentId, now, cancellationToken);
        if (claimed is null)
            return false;

        _logger.LogInformation(
            "Document processing started. CorrelationId={CorrelationId} DocumentId={DocumentId} " +
            "OrganizationId={OrganizationId} Status={Status}",
            _correlationContext.CorrelationId,
            claimed.DocumentId,
            claimed.OrganizationId,
            claimed.ProcessingStatus);

        await WriteAuditAsync(
            AuditEventTypes.DocumentProcessingStarted,
            "Document processing started.",
            claimed.OrganizationId,
            claimed.DocumentId,
            cancellationToken);

        var started = DateTimeOffset.UtcNow;
        try
        {
            await using var transaction = await _transactionFactory.BeginAsync(cancellationToken);
            try
            {
                foreach (var step in _steps)
                    await step.ExecuteAsync(claimed, cancellationToken);

                var completedAt = DateTimeOffset.UtcNow;
                await _repository.MarkProcessedAsync(claimed.DocumentId, completedAt, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Document processing succeeded. CorrelationId={CorrelationId} DocumentId={DocumentId} " +
                    "OrganizationId={OrganizationId} DurationMs={DurationMs}",
                    _correlationContext.CorrelationId,
                    claimed.DocumentId,
                    claimed.OrganizationId,
                    (long)(completedAt - started).TotalMilliseconds);

                await WriteAuditAsync(
                    AuditEventTypes.DocumentProcessingSucceeded,
                    "Document processing succeeded.",
                    claimed.OrganizationId,
                    claimed.DocumentId,
                    cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var failedAt = DateTimeOffset.UtcNow;
            var safeReason = BuildSafeFailureReason(ex);

            await _repository.MarkFailedAsync(claimed.DocumentId, safeReason, failedAt, cancellationToken);

            _logger.LogWarning(
                "Document processing failed. CorrelationId={CorrelationId} DocumentId={DocumentId} " +
                "OrganizationId={OrganizationId} DurationMs={DurationMs}",
                _correlationContext.CorrelationId,
                claimed.DocumentId,
                claimed.OrganizationId,
                (long)(failedAt - started).TotalMilliseconds);

            await WriteAuditAsync(
                AuditEventTypes.DocumentProcessingFailed,
                "Document processing failed.",
                claimed.OrganizationId,
                claimed.DocumentId,
                cancellationToken,
                AuditSeverity.Warning);
        }

        return true;
    }

    private static string BuildSafeFailureReason(Exception ex)
    {
        // Never use ex.ToString() — it includes the full stack trace.
        // Never include absolute paths, storage locations, or document content.
        // Use only the short exception message, trimmed and capped.
        var message = ex.Message?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(message))
            return "Document processing failed.";

        return message.Length <= MaxFailureReasonLength
            ? message
            : message[..MaxFailureReasonLength];
    }

    private async Task WriteAuditAsync(
        string eventType,
        string message,
        Guid organizationId,
        Guid documentId,
        CancellationToken ct,
        AuditSeverity severity = AuditSeverity.Info)
    {
        try
        {
            await _auditEventWriter.WriteAsync(
                new AuditEvent(
                    eventType,
                    message,
                    severity,
                    _correlationContext.CorrelationId,
                    organizationId,
                    null,
                    "Document",
                    documentId),
                ct);
        }
        catch
        {
            _logger.LogWarning(
                "Audit write failed. EventType={EventType} CorrelationId={CorrelationId}",
                eventType,
                _correlationContext.CorrelationId);
        }
    }
}

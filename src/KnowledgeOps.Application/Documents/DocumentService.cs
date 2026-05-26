using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Documents;

public sealed class DocumentService(
    IDocumentRepository repository,
    IAuditEventWriter auditEventWriter,
    ICorrelationContext correlationContext,
    ILogger<DocumentService> logger)
{
    public Task<IReadOnlyList<ManagedDocument>> ListAsync(
        DocumentActor actor,
        CancellationToken ct = default) =>
        repository.ListAsync(actor.OrganizationId, ct);

    public async Task<ManagedDocument> GetAsync(
        DocumentActor actor,
        Guid documentId,
        CancellationToken ct = default) =>
        await repository.FindAsync(documentId, actor.OrganizationId, ct)
        ?? throw new ApplicationNotFoundException();

    public async Task<ManagedDocument> GetProcessingStatusAsync(
        DocumentActor actor,
        Guid documentId,
        CancellationToken ct = default) =>
        await repository.FindAsync(documentId, actor.OrganizationId, ct)
        ?? throw new ApplicationNotFoundException();

    public async Task<ManagedDocument> DisableRetrievalAsync(
        DocumentActor actor,
        Guid documentId,
        CancellationToken ct = default)
    {
        var result = await repository.DisableRetrievalAsync(
            documentId,
            actor.OrganizationId,
            DateTimeOffset.UtcNow,
            ct)
            ?? throw new ApplicationNotFoundException();

        if (result.WasChanged)
        {
            await AuditAsync(
                AuditEventTypes.DocumentRetrievalDisabled,
                "Document retrieval disabled.",
                actor,
                documentId,
                ct);
        }

        return result.Document;
    }

    private async Task AuditAsync(
        string eventType,
        string message,
        DocumentActor actor,
        Guid targetDocumentId,
        CancellationToken ct,
        AuditSeverity severity = AuditSeverity.Info)
    {
        try
        {
            await auditEventWriter.WriteAsync(
                new AuditEvent(
                    eventType,
                    message,
                    severity,
                    correlationContext.CorrelationId,
                    actor.OrganizationId,
                    actor.UserId,
                    "Document",
                    targetDocumentId),
                ct);
        }
        catch
        {
            logger.LogWarning(
                "Audit write failed. EventType={EventType} CorrelationId={CorrelationId}",
                eventType,
                correlationContext.CorrelationId);
        }
    }
}

using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Documents;

public sealed class DocumentService(
    IDocumentRepository repository,
    IDocumentStorage storage,
    IAuditEventWriter auditEventWriter,
    ICorrelationContext correlationContext,
    ILogger<DocumentService> logger)
{
    private static readonly long MaxFileSizeBytes = 10L * 1024 * 1024;

    private static readonly IReadOnlySet<string> AllowedExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".txt", ".md", ".markdown", ".docx" };

    private static readonly IReadOnlySet<string> AllowedContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "text/plain",
            "text/markdown",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

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

    public async Task<ManagedDocument> EnableRetrievalAsync(
        DocumentActor actor,
        Guid documentId,
        CancellationToken ct = default)
    {
        ManagedDocument document;
        bool wasChanged;
        try
        {
            var result = await repository.EnableRetrievalAsync(
                documentId,
                actor.OrganizationId,
                DateTimeOffset.UtcNow,
                ct)
                ?? throw new ApplicationNotFoundException();

            document = result.Document;
            wasChanged = result.WasChanged;
        }
        catch (InvalidOperationException)
        {
            throw new ApplicationConflictException();
        }

        if (wasChanged)
        {
            await AuditAsync(
                AuditEventTypes.DocumentRetrievalEnabled,
                "Document retrieval enabled.",
                actor,
                documentId,
                ct);
        }

        return document;
    }

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

    public async Task<ManagedDocument> UploadAsync(
        DocumentActor actor,
        UploadDocumentCommand command,
        CancellationToken ct = default)
    {
        var validationErrors = ValidateUpload(command);
        if (validationErrors.Count > 0)
        {
            logger.LogInformation(
                "Document upload rejected. CorrelationId={CorrelationId} OrganizationId={OrganizationId} " +
                "UserId={UserId} FileName={FileName} ContentType={ContentType} FileSizeBytes={FileSizeBytes} " +
                "Result=Rejected",
                correlationContext.CorrelationId,
                actor.OrganizationId,
                actor.UserId,
                MakeSafeLogName(command.OriginalFileName),
                command.ContentType,
                command.FileSizeBytes);

            await AuditAsync(
                AuditEventTypes.DocumentUploadRejected,
                "Document upload rejected due to validation failure.",
                actor,
                null,
                ct,
                AuditSeverity.Info);

            throw new ApplicationValidationException(validationErrors);
        }

        var safeFileName = MakeSafeFileName(command.OriginalFileName);

        StoredDocumentReference storageRef;
        try
        {
            storageRef = await storage.StoreAsync(command.FileStream, safeFileName, command.ContentType, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Document upload failed during storage. CorrelationId={CorrelationId} OrganizationId={OrganizationId} " +
                "UserId={UserId} FileName={FileName} Result=StorageFailed",
                correlationContext.CorrelationId,
                actor.OrganizationId,
                actor.UserId,
                safeFileName);

            await AuditAsync(
                AuditEventTypes.DocumentUploadFailed,
                "Document upload failed during storage.",
                actor,
                null,
                ct,
                AuditSeverity.Warning);

            throw new ApplicationServiceUnavailableException();
        }

        var documentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var newDocument = new NewManagedDocument(
            documentId,
            actor.OrganizationId,
            safeFileName,
            command.Title.Trim(),
            command.ContentType,
            command.FileSizeBytes,
            storageRef.Location,
            actor.UserId,
            now,
            now);

        ManagedDocument result;
        try
        {
            result = await repository.CreateAsync(newDocument, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Document upload failed during metadata persistence. CorrelationId={CorrelationId} " +
                "OrganizationId={OrganizationId} UserId={UserId} FileName={FileName} Result=PersistenceFailed",
                correlationContext.CorrelationId,
                actor.OrganizationId,
                actor.UserId,
                safeFileName);

            try
            {
                await storage.DeleteAsync(storageRef.Location, ct);
            }
            catch (Exception cleanupEx)
            {
                logger.LogWarning(
                    cleanupEx,
                    "Best-effort cleanup of stored file failed after metadata persistence failure. " +
                    "CorrelationId={CorrelationId}",
                    correlationContext.CorrelationId);
            }

            await AuditAsync(
                AuditEventTypes.DocumentUploadFailed,
                "Document upload failed during metadata persistence.",
                actor,
                null,
                ct,
                AuditSeverity.Warning);

            throw new ApplicationServiceUnavailableException();
        }

        logger.LogInformation(
            "Document upload accepted. CorrelationId={CorrelationId} OrganizationId={OrganizationId} " +
            "UserId={UserId} FileName={FileName} ContentType={ContentType} FileSizeBytes={FileSizeBytes} " +
            "DocumentId={DocumentId} Result=Accepted",
            correlationContext.CorrelationId,
            actor.OrganizationId,
            actor.UserId,
            safeFileName,
            command.ContentType,
            command.FileSizeBytes,
            documentId);

        await AuditAsync(
            AuditEventTypes.DocumentUploadAccepted,
            "Document uploaded.",
            actor,
            documentId,
            ct);

        return result;
    }

    private static List<ApplicationValidationItem> ValidateUpload(UploadDocumentCommand command)
    {
        var errors = new List<ApplicationValidationItem>();

        if (string.IsNullOrWhiteSpace(command.Title))
            errors.Add(new("title", "Title is required."));

        if (command.FileSizeBytes <= 0)
        {
            errors.Add(new("file", "File is required and must not be empty."));
        }
        else if (command.FileSizeBytes > MaxFileSizeBytes)
        {
            errors.Add(new("file", "File size must not exceed 10 MB."));
        }

        if (string.IsNullOrWhiteSpace(command.OriginalFileName))
        {
            errors.Add(new("file", "File name is required."));
        }
        else
        {
            var ext = System.IO.Path.GetExtension(command.OriginalFileName);
            if (!AllowedExtensions.Contains(ext))
                errors.Add(new("file", "File type is not supported. Allowed formats: PDF, TXT, MD, DOCX."));
        }

        if (!string.IsNullOrWhiteSpace(command.ContentType))
        {
            var baseType = command.ContentType.Split(';')[0].Trim();
            if (!AllowedContentTypes.Contains(baseType))
                errors.Add(new("file", "Content type is not supported."));
        }
        else
        {
            errors.Add(new("file", "Content type is required."));
        }

        return errors;
    }

    private static string MakeSafeFileName(string originalFileName)
    {
        var name = System.IO.Path.GetFileName(originalFileName);
        if (string.IsNullOrWhiteSpace(name))
            return "document";

        var safe = string.Concat(name.Select(c =>
            char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_'));

        return safe.Length > 0 ? safe : "document";
    }

    private static string MakeSafeLogName(string? originalFileName) =>
        string.IsNullOrWhiteSpace(originalFileName)
            ? "(unknown)"
            : System.IO.Path.GetFileName(originalFileName);

    private async Task AuditAsync(
        string eventType,
        string message,
        DocumentActor actor,
        Guid? targetDocumentId,
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

using KnowledgeOps.Api.Authorization;
using KnowledgeOps.Api.Controllers.Models;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Documents;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Api.Controllers;

[ApiController]
[Route("api/v1/documents")]
public sealed class DocumentsController(DocumentService service, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(KnowledgeOpsPermissions.Documents.View)]
    public async Task<ActionResult<IReadOnlyList<DocumentResponse>>> List(CancellationToken ct)
    {
        var documents = await service.ListAsync(Actor(), ct);
        return Ok(documents.Select(ToResponse).ToArray());
    }

    [HttpGet("{documentId:guid}")]
    [RequirePermission(KnowledgeOpsPermissions.Documents.View)]
    public async Task<ActionResult<DocumentResponse>> Get(Guid documentId, CancellationToken ct) =>
        Ok(ToResponse(await service.GetAsync(Actor(), documentId, ct)));

    [HttpGet("{documentId:guid}/processing-status")]
    [RequirePermission(KnowledgeOpsPermissions.Documents.ViewProcessingStatus)]
    public async Task<ActionResult<DocumentProcessingStatusResponse>> GetProcessingStatus(
        Guid documentId,
        CancellationToken ct) =>
        Ok(ToStatusResponse(await service.GetProcessingStatusAsync(Actor(), documentId, ct)));

    [HttpPost("{documentId:guid}/disable")]
    [RequirePermission(KnowledgeOpsPermissions.Documents.Disable)]
    public async Task<ActionResult<DocumentResponse>> DisableRetrieval(
        Guid documentId,
        CancellationToken ct) =>
        Ok(ToResponse(await service.DisableRetrievalAsync(Actor(), documentId, ct)));

    private DocumentActor Actor() => new(currentUser.UserId, currentUser.OrganizationId);

    private static DocumentResponse ToResponse(ManagedDocument doc) =>
        new()
        {
            DocumentId = doc.DocumentId,
            FileName = doc.FileName,
            Title = doc.Title,
            ContentType = doc.ContentType,
            FileSizeBytes = doc.FileSizeBytes,
            ProcessingStatus = doc.ProcessingStatus.ToString(),
            FailureReason = doc.FailureReason,
            IsRetrievalEnabled = doc.IsRetrievalEnabled,
            UploadedByUserId = doc.UploadedByUserId,
            UploadedAt = doc.UploadedAt,
            ProcessedAt = doc.ProcessedAt
        };

    private static DocumentProcessingStatusResponse ToStatusResponse(ManagedDocument doc) =>
        new()
        {
            DocumentId = doc.DocumentId,
            ProcessingStatus = doc.ProcessingStatus.ToString(),
            FailureReason = doc.FailureReason,
            IsRetrievalEnabled = doc.IsRetrievalEnabled,
            UploadedAt = doc.UploadedAt,
            ProcessingStartedAt = doc.ProcessingStartedAt,
            ProcessedAt = doc.ProcessedAt
        };
}

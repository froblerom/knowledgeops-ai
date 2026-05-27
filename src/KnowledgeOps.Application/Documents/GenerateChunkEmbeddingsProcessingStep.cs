using System.Text.Json;
using KnowledgeOps.Application.Embeddings;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Documents;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Documents;

internal sealed class GenerateChunkEmbeddingsProcessingStep(
    IDocumentChunkRepository chunkRepository,
    IEmbeddingProvider embeddingProvider,
    IChunkEmbeddingRepository embeddingRepository,
    IAuditEventWriter auditEventWriter,
    ICorrelationContext correlationContext,
    ILogger<GenerateChunkEmbeddingsProcessingStep> logger) : IDocumentProcessingStep
{
    private const int MaxFailureReasonLength = 200;

    public async Task ExecuteAsync(ManagedDocument document, CancellationToken cancellationToken = default)
    {
        var chunks = await chunkRepository.GetChunksForDocumentAsync(document.DocumentId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var records = new List<ChunkEmbeddingRecord>(chunks.Count);
        var failureCount = 0;

        foreach (var chunk in chunks)
        {
            ChunkEmbeddingRecord record;
            try
            {
                var request = new EmbeddingRequest(
                    chunk.Text,
                    embeddingProvider.DefaultModelName,
                    embeddingProvider.DefaultDimensions);

                var response = await embeddingProvider.GenerateAsync(request, cancellationToken);

                if (response.Vector is null || response.Vector.Length == 0)
                    throw new DocumentEmbeddingException("Embedding vector was invalid.");

                record = new ChunkEmbeddingRecord(
                    EmbeddingId: Guid.NewGuid(),
                    ChunkId: chunk.ChunkId,
                    OrganizationId: document.OrganizationId,
                    ProviderName: embeddingProvider.ProviderName,
                    ModelName: embeddingProvider.DefaultModelName,
                    VectorData: JsonSerializer.Serialize(response.Vector),
                    VectorDimensions: response.Vector.Length,
                    Status: EmbeddingStatus.Ready,
                    FailureReason: null,
                    CreatedAt: now,
                    UpdatedAt: now);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failureCount++;

                logger.LogWarning(
                    "Embedding generation failed. DocumentId={DocumentId} ChunkIndex={ChunkIndex}",
                    document.DocumentId,
                    chunk.ChunkIndex);

                record = new ChunkEmbeddingRecord(
                    EmbeddingId: Guid.NewGuid(),
                    ChunkId: chunk.ChunkId,
                    OrganizationId: document.OrganizationId,
                    ProviderName: embeddingProvider.ProviderName,
                    ModelName: embeddingProvider.DefaultModelName,
                    VectorData: null,
                    VectorDimensions: null,
                    Status: EmbeddingStatus.Failed,
                    FailureReason: BuildSafeFailureReason(ex),
                    CreatedAt: now,
                    UpdatedAt: now);
            }

            records.Add(record);
        }

        await embeddingRepository.SaveEmbeddingsAsync(records, cancellationToken);

        await TryWriteAuditAsync(document, failureCount, records.Count);

        logger.LogInformation(
            "Embedding generation complete. DocumentId={DocumentId} Total={Total} Failed={Failed}",
            document.DocumentId,
            records.Count,
            failureCount);
    }

    private async Task TryWriteAuditAsync(ManagedDocument document, int failureCount, int totalCount)
    {
        try
        {
            var succeeded = failureCount == 0;
            var message = succeeded
                ? $"Embedding generation succeeded. Chunks={totalCount}"
                : $"Embedding generation partial failure. Failed={failureCount} Total={totalCount}";

            await auditEventWriter.WriteAsync(new AuditEvent(
                EventType: succeeded ? AuditEventTypes.EmbeddingGenerationSucceeded : AuditEventTypes.EmbeddingGenerationFailed,
                Message: message,
                Severity: succeeded ? AuditSeverity.Info : AuditSeverity.Warning,
                CorrelationId: correlationContext.CorrelationId,
                OrganizationId: document.OrganizationId,
                EntityType: "Document",
                EntityId: document.DocumentId));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Audit write failed for embedding generation. DocumentId={DocumentId}",
                document.DocumentId);
        }
    }

    private static string BuildSafeFailureReason(Exception ex)
    {
        var message = ex.Message?.Trim() ?? string.Empty;
        return message.Length <= MaxFailureReasonLength
            ? message
            : message[..MaxFailureReasonLength];
    }
}

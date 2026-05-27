using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Documents;

internal sealed class ExtractAndChunkDocumentProcessingStep(
    IDocumentStorage storage,
    IDocumentTextExtractor extractor,
    IDocumentChunker chunker,
    IDocumentChunkRepository chunkRepository,
    ILogger<ExtractAndChunkDocumentProcessingStep> logger) : IDocumentProcessingStep
{
    public async Task ExecuteAsync(ManagedDocument document, CancellationToken cancellationToken = default)
    {
        if (!extractor.Supports(document.ContentType))
            throw new DocumentExtractionException("Unsupported document format for text extraction.");

        await using var fileStream = await storage.OpenReadAsync(document.StorageLocation, cancellationToken);

        var text = await extractor.ExtractAsync(fileStream, cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
            throw new DocumentExtractionException("No usable text could be extracted from the document.");

        var chunks = chunker.Chunk(text);

        if (chunks.Count == 0)
            throw new DocumentChunkingException("No usable text could be extracted from the document.");

        var now = DateTimeOffset.UtcNow;
        var records = chunks
            .Select(c => new DocumentChunkRecord(
                ChunkId: Guid.NewGuid(),
                DocumentId: document.DocumentId,
                OrganizationId: document.OrganizationId,
                ChunkIndex: c.Index,
                Text: c.Text,
                CharacterLength: c.CharacterLength,
                TokenEstimate: c.TokenEstimate,
                CreatedAt: now))
            .ToArray();

        await chunkRepository.SaveChunksAsync(records, cancellationToken);

        logger.LogInformation(
            "Document processed. DocumentId={DocumentId} ChunkCount={ChunkCount}",
            document.DocumentId,
            records.Length);
    }
}

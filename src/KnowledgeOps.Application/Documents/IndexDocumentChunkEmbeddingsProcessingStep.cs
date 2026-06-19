using KnowledgeOps.Application.Retrieval;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Documents;

/// <summary>
/// Processing step 3: marks generated chunk embeddings as indexed so the
/// local vector store's SearchAsync eligibility filter can find them.
/// Must run after GenerateChunkEmbeddingsProcessingStep (step 2) within the
/// same document processing transaction.
/// </summary>
internal sealed class IndexDocumentChunkEmbeddingsProcessingStep(
    IRetrievalIndex retrievalIndex,
    ILogger<IndexDocumentChunkEmbeddingsProcessingStep> logger) : IDocumentProcessingStep
{
    public async Task ExecuteAsync(ManagedDocument document, CancellationToken cancellationToken = default)
    {
        var result = await retrievalIndex.IndexAsync(
            new VectorIndexRequest(document.OrganizationId, document.DocumentId),
            cancellationToken);

        logger.LogInformation(
            "Chunk embedding indexing complete. DocumentId={DocumentId} " +
            "Eligible={Eligible} Indexed={Indexed} Failed={Failed}",
            document.DocumentId,
            result.EligibleEmbeddingCount,
            result.IndexedCount,
            result.FailedCount);

        if (result.EligibleEmbeddingCount == 0)
            throw new DocumentEmbeddingException(
                "No ready embeddings were available to index. Embedding generation may have failed for all chunks.");

        if (result.IndexedCount == 0)
            throw new DocumentEmbeddingException(
                "Chunk embedding indexing failed. All eligible embeddings had invalid vector data.");
    }
}

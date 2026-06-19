using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Retrieval;
using KnowledgeOps.Domain.Documents;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Documents;

public sealed class IndexDocumentChunkEmbeddingsProcessingStepTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid DocId = Guid.NewGuid();

    private static ManagedDocument MakeDocument(
        DocumentProcessingStatus status = DocumentProcessingStatus.Processing,
        bool isRetrievalEnabled = false) =>
        new(DocId, OrgId, "policy.txt", "Policy", "text/plain", 42,
            "local://policy.txt", status, null, isRetrievalEnabled,
            Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);

    [Fact]
    public async Task ExecuteAsync_WhenIndexingSucceeds_DoesNotThrow()
    {
        var index = new FakeRetrievalIndex(eligible: 2, indexed: 2, failed: 0);
        var step = new IndexDocumentChunkEmbeddingsProcessingStep(
            index, NullLogger<IndexDocumentChunkEmbeddingsProcessingStep>.Instance);

        // Should not throw — 2 embeddings found, 2 indexed
        await step.ExecuteAsync(MakeDocument());
    }

    [Fact]
    public async Task ExecuteAsync_PassesDocumentIdAndOrgIdToIndex()
    {
        var index = new FakeRetrievalIndex(eligible: 1, indexed: 1, failed: 0);
        var step = new IndexDocumentChunkEmbeddingsProcessingStep(
            index, NullLogger<IndexDocumentChunkEmbeddingsProcessingStep>.Instance);

        await step.ExecuteAsync(MakeDocument());

        Assert.Equal(DocId, index.LastRequest?.DocumentId);
        Assert.Equal(OrgId, index.LastRequest?.OrganizationId);
    }

    [Fact]
    public async Task ExecuteAsync_WorksWhileDocumentIsInProcessingStatus()
    {
        var index = new FakeRetrievalIndex(eligible: 1, indexed: 1, failed: 0);
        var step = new IndexDocumentChunkEmbeddingsProcessingStep(
            index, NullLogger<IndexDocumentChunkEmbeddingsProcessingStep>.Instance);

        // Document is still Processing (not yet Processed) — must not throw
        await step.ExecuteAsync(MakeDocument(status: DocumentProcessingStatus.Processing));
    }

    [Fact]
    public async Task ExecuteAsync_WorksWhenRetrievalIsDisabled()
    {
        var index = new FakeRetrievalIndex(eligible: 1, indexed: 1, failed: 0);
        var step = new IndexDocumentChunkEmbeddingsProcessingStep(
            index, NullLogger<IndexDocumentChunkEmbeddingsProcessingStep>.Instance);

        // IsRetrievalEnabled = false (default for new documents) — indexing must not require it
        await step.ExecuteAsync(MakeDocument(isRetrievalEnabled: false));
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsWhenNoEligibleEmbeddingsFound()
    {
        var index = new FakeRetrievalIndex(eligible: 0, indexed: 0, failed: 0);
        var step = new IndexDocumentChunkEmbeddingsProcessingStep(
            index, NullLogger<IndexDocumentChunkEmbeddingsProcessingStep>.Instance);

        // Eligible=0 means embedding generation failed for all chunks — must fail the document
        await Assert.ThrowsAsync<DocumentEmbeddingException>(() =>
            step.ExecuteAsync(MakeDocument()));
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsWhenAllEligibleEmbeddingsFailedVectorValidation()
    {
        var index = new FakeRetrievalIndex(eligible: 2, indexed: 0, failed: 2);
        var step = new IndexDocumentChunkEmbeddingsProcessingStep(
            index, NullLogger<IndexDocumentChunkEmbeddingsProcessingStep>.Instance);

        // Eligible=2 but indexed=0 (all vectors invalid) — must fail the document
        await Assert.ThrowsAsync<DocumentEmbeddingException>(() =>
            step.ExecuteAsync(MakeDocument()));
    }

    [Fact]
    public async Task ExecuteAsync_PartialIndexingSuccessDoesNotThrow()
    {
        var index = new FakeRetrievalIndex(eligible: 3, indexed: 2, failed: 1);
        var step = new IndexDocumentChunkEmbeddingsProcessingStep(
            index, NullLogger<IndexDocumentChunkEmbeddingsProcessingStep>.Instance);

        // 2 indexed, 1 failed — at least some indexed, so this is acceptable
        await step.ExecuteAsync(MakeDocument());
    }

    private sealed class FakeRetrievalIndex(int eligible, int indexed, int failed) : IRetrievalIndex
    {
        private static readonly RetrievalProviderMetadata Metadata = new("Fake", "Fake", "InMemory");

        public VectorIndexRequest? LastRequest { get; private set; }

        public Task<VectorIndexResult> IndexAsync(
            VectorIndexRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new VectorIndexResult(eligible, indexed, failed, 0, Metadata));
        }
    }
}

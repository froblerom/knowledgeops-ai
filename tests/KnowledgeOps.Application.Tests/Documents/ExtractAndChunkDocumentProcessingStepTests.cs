using KnowledgeOps.Application.Documents;
using KnowledgeOps.Domain.Documents;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Documents;

public sealed class ExtractAndChunkDocumentProcessingStepTests
{
    private static readonly DateTimeOffset UploadedAt = new(2026, 5, 27, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_UnsupportedContentType_ThrowsDocumentExtractionException()
    {
        var step = BuildStep(extractor: new FixedExtractor(supports: false, text: string.Empty));
        var doc = MakeDocument(contentType: "application/pdf");

        var ex = await Assert.ThrowsAsync<DocumentExtractionException>(
            () => step.ExecuteAsync(doc));

        Assert.Equal("Unsupported document format for text extraction.", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyExtractedText_ThrowsDocumentExtractionException()
    {
        var step = BuildStep(extractor: new FixedExtractor(supports: true, text: "   "));
        var doc = MakeDocument();

        var ex = await Assert.ThrowsAsync<DocumentExtractionException>(
            () => step.ExecuteAsync(doc));

        Assert.Equal("No usable text could be extracted from the document.", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ValidDocument_SavesChunksToRepository()
    {
        var chunkRepo = new CapturingChunkRepository();
        var step = BuildStep(
            extractor: new FixedExtractor(supports: true, text: "Hello world."),
            chunker: new FixedChunker([new TextChunk(0, "Hello world.", 12, 3)]),
            chunkRepository: chunkRepo);
        var doc = MakeDocument();

        await step.ExecuteAsync(doc);

        Assert.Single(chunkRepo.Saved);
        Assert.Equal(doc.DocumentId, chunkRepo.Saved[0].DocumentId);
        Assert.Equal(doc.OrganizationId, chunkRepo.Saved[0].OrganizationId);
        Assert.Equal(0, chunkRepo.Saved[0].ChunkIndex);
        Assert.Equal("Hello world.", chunkRepo.Saved[0].Text);
    }

    [Fact]
    public async Task ExecuteAsync_OpenReadsStorageLocation()
    {
        var storage = new RecordingStorage();
        var step = BuildStep(
            storage: storage,
            extractor: new FixedExtractor(supports: true, text: "Content here."),
            chunker: new FixedChunker([new TextChunk(0, "Content here.", 13, 4)]));
        var doc = MakeDocument(storageLocation: "local://test/policy.pdf");

        await step.ExecuteAsync(doc);

        Assert.Equal("local://test/policy.pdf", storage.LastOpenedReference);
    }

    [Fact]
    public async Task ExecuteAsync_StorageLocationIsNotLogged()
    {
        // Verify that executing the step does not propagate storage location in any exception message.
        var step = BuildStep(
            extractor: new FixedExtractor(supports: false, text: string.Empty));
        var doc = MakeDocument(storageLocation: "local://sensitive/secret/path.txt");

        var ex = await Assert.ThrowsAsync<DocumentExtractionException>(
            () => step.ExecuteAsync(doc));

        Assert.DoesNotContain("sensitive", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static ExtractAndChunkDocumentProcessingStep BuildStep(
        RecordingStorage? storage = null,
        IDocumentTextExtractor? extractor = null,
        IDocumentChunker? chunker = null,
        CapturingChunkRepository? chunkRepository = null) =>
        new(
            storage ?? new RecordingStorage(),
            extractor ?? new FixedExtractor(supports: true, text: "default text"),
            chunker ?? new FixedChunker([new TextChunk(0, "default text", 12, 3)]),
            chunkRepository ?? new CapturingChunkRepository(),
            NullLogger<ExtractAndChunkDocumentProcessingStep>.Instance);

    private static ManagedDocument MakeDocument(
        string contentType = "text/plain",
        string storageLocation = "local://test/policy.txt") =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "policy.txt",
            "Test Document",
            contentType,
            42,
            storageLocation,
            DocumentProcessingStatus.Processing,
            null,
            false,
            Guid.NewGuid(),
            UploadedAt,
            UploadedAt,
            null,
            UploadedAt,
            UploadedAt,
            null);

    // ──────────────────────────────────────────────────────────────
    // Fakes
    // ──────────────────────────────────────────────────────────────

    private sealed class RecordingStorage : IDocumentStorage
    {
        public string? LastOpenedReference { get; private set; }

        public Task<StoredDocumentReference> StoreAsync(
            Stream fileStream, string safeFileName, string contentType, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Stream> OpenReadAsync(string storageReference, CancellationToken ct = default)
        {
            LastOpenedReference = storageReference;
            return Task.FromResult<Stream>(new MemoryStream("hello"u8.ToArray()));
        }

        public Task DeleteAsync(string storageReference, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class FixedExtractor(bool supports, string text) : IDocumentTextExtractor
    {
        public bool Supports(string contentType) => supports;

        public Task<string> ExtractAsync(Stream fileStream, CancellationToken ct = default) =>
            Task.FromResult(text);
    }

    private sealed class FixedChunker(IReadOnlyList<TextChunk> chunks) : IDocumentChunker
    {
        public IReadOnlyList<TextChunk> Chunk(string text) => chunks;
    }

    private sealed class CapturingChunkRepository : IDocumentChunkRepository
    {
        public List<DocumentChunkRecord> Saved { get; } = [];

        public Task SaveChunksAsync(IReadOnlyList<DocumentChunkRecord> chunks, CancellationToken ct = default)
        {
            Saved.AddRange(chunks);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DocumentChunkRecord>> GetChunksForDocumentAsync(Guid documentId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DocumentChunkRecord>>(Saved.Where(c => c.DocumentId == documentId).ToList());
    }
}

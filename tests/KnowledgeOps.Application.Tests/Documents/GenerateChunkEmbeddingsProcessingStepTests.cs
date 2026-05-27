using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Embeddings;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Documents;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Documents;

public sealed class GenerateChunkEmbeddingsProcessingStepTests
{
    private static readonly DateTimeOffset UploadedAt = new(2026, 5, 27, 10, 0, 0, TimeSpan.Zero);

    // ──────────────────────────────────────────────────────────────
    // Happy path
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ValidChunks_SavesReadyEmbeddings()
    {
        var chunkRepo = new FixedChunkRepository([MakeChunkRecord()]);
        var embeddingRepo = new CapturingEmbeddingRepository();
        var step = BuildStep(chunkRepository: chunkRepo, embeddingRepository: embeddingRepo);

        await step.ExecuteAsync(MakeDocument());

        Assert.Single(embeddingRepo.Saved);
        Assert.Equal(EmbeddingStatus.Ready, embeddingRepo.Saved[0].Status);
        Assert.Null(embeddingRepo.Saved[0].FailureReason);
    }

    [Fact]
    public async Task ExecuteAsync_ValidChunks_VectorDataIsNonEmpty()
    {
        var chunkRepo = new FixedChunkRepository([MakeChunkRecord()]);
        var embeddingRepo = new CapturingEmbeddingRepository();
        var step = BuildStep(chunkRepository: chunkRepo, embeddingRepository: embeddingRepo);

        await step.ExecuteAsync(MakeDocument());

        Assert.NotNull(embeddingRepo.Saved[0].VectorData);
        Assert.NotEmpty(embeddingRepo.Saved[0].VectorData!);
    }

    [Fact]
    public async Task ExecuteAsync_ValidChunks_DimensionsMatchProviderDefault()
    {
        var provider = new FixedEmbeddingProvider(dimensions: 8);
        var chunkRepo = new FixedChunkRepository([MakeChunkRecord()]);
        var embeddingRepo = new CapturingEmbeddingRepository();
        var step = BuildStep(chunkRepository: chunkRepo, embeddingRepository: embeddingRepo, provider: provider);

        await step.ExecuteAsync(MakeDocument());

        Assert.Equal(8, embeddingRepo.Saved[0].VectorDimensions);
    }

    [Fact]
    public async Task ExecuteAsync_ValidChunks_OrganizationIdMatchesDocument()
    {
        var doc = MakeDocument();
        var chunkRepo = new FixedChunkRepository([MakeChunkRecord(documentId: doc.DocumentId, organizationId: doc.OrganizationId)]);
        var embeddingRepo = new CapturingEmbeddingRepository();
        var step = BuildStep(chunkRepository: chunkRepo, embeddingRepository: embeddingRepo);

        await step.ExecuteAsync(doc);

        Assert.Equal(doc.OrganizationId, embeddingRepo.Saved[0].OrganizationId);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleChunks_SavesOneEmbeddingPerChunk()
    {
        var doc = MakeDocument();
        var chunks = new[]
        {
            MakeChunkRecord(documentId: doc.DocumentId, chunkIndex: 0),
            MakeChunkRecord(documentId: doc.DocumentId, chunkIndex: 1),
            MakeChunkRecord(documentId: doc.DocumentId, chunkIndex: 2)
        };
        var chunkRepo = new FixedChunkRepository(chunks);
        var embeddingRepo = new CapturingEmbeddingRepository();
        var step = BuildStep(chunkRepository: chunkRepo, embeddingRepository: embeddingRepo);

        await step.ExecuteAsync(doc);

        Assert.Equal(3, embeddingRepo.Saved.Count);
        Assert.All(embeddingRepo.Saved, e => Assert.Equal(EmbeddingStatus.Ready, e.Status));
    }

    // ──────────────────────────────────────────────────────────────
    // Failure handling
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ProviderThrows_SavesFailedEmbedding()
    {
        var chunkRepo = new FixedChunkRepository([MakeChunkRecord()]);
        var embeddingRepo = new CapturingEmbeddingRepository();
        var provider = new FailingEmbeddingProvider("Provider unavailable.");
        var step = BuildStep(chunkRepository: chunkRepo, embeddingRepository: embeddingRepo, provider: provider);

        await step.ExecuteAsync(MakeDocument());

        Assert.Single(embeddingRepo.Saved);
        Assert.Equal(EmbeddingStatus.Failed, embeddingRepo.Saved[0].Status);
        Assert.Equal("Provider unavailable.", embeddingRepo.Saved[0].FailureReason);
    }

    [Fact]
    public async Task ExecuteAsync_ProviderReturnsEmptyVector_SavesFailedEmbedding()
    {
        var chunkRepo = new FixedChunkRepository([MakeChunkRecord()]);
        var embeddingRepo = new CapturingEmbeddingRepository();
        var provider = new FixedEmbeddingProvider(vector: []);
        var step = BuildStep(chunkRepository: chunkRepo, embeddingRepository: embeddingRepo, provider: provider);

        await step.ExecuteAsync(MakeDocument());

        Assert.Single(embeddingRepo.Saved);
        Assert.Equal(EmbeddingStatus.Failed, embeddingRepo.Saved[0].Status);
        Assert.Equal("Embedding vector was invalid.", embeddingRepo.Saved[0].FailureReason);
    }

    [Fact]
    public async Task ExecuteAsync_PartialFailure_SavesMixedStatusRecords()
    {
        var doc = MakeDocument();
        var chunks = new[]
        {
            MakeChunkRecord(documentId: doc.DocumentId, chunkIndex: 0),
            MakeChunkRecord(documentId: doc.DocumentId, chunkIndex: 1)
        };
        var chunkRepo = new FixedChunkRepository(chunks);
        var embeddingRepo = new CapturingEmbeddingRepository();
        // Fails only on second call
        var provider = new CountingFailingProvider(failOnCallNumber: 2);
        var step = BuildStep(chunkRepository: chunkRepo, embeddingRepository: embeddingRepo, provider: provider);

        await step.ExecuteAsync(doc);

        Assert.Equal(2, embeddingRepo.Saved.Count);
        Assert.Equal(EmbeddingStatus.Ready, embeddingRepo.Saved[0].Status);
        Assert.Equal(EmbeddingStatus.Failed, embeddingRepo.Saved[1].Status);
    }

    [Fact]
    public async Task ExecuteAsync_FailureReason_TruncatedToMaxLength()
    {
        var longMessage = new string('x', 500);
        var chunkRepo = new FixedChunkRepository([MakeChunkRecord()]);
        var embeddingRepo = new CapturingEmbeddingRepository();
        var provider = new FailingEmbeddingProvider(longMessage);
        var step = BuildStep(chunkRepository: chunkRepo, embeddingRepository: embeddingRepo, provider: provider);

        await step.ExecuteAsync(MakeDocument());

        Assert.NotNull(embeddingRepo.Saved[0].FailureReason);
        Assert.True(embeddingRepo.Saved[0].FailureReason!.Length <= 200);
    }

    [Fact]
    public async Task ExecuteAsync_FailureReason_DoesNotContainChunkText()
    {
        var chunkRepo = new FixedChunkRepository([MakeChunkRecord(text: "super-sensitive-content")]);
        var embeddingRepo = new CapturingEmbeddingRepository();
        var provider = new FailingEmbeddingProvider("Provider error.");
        var step = BuildStep(chunkRepository: chunkRepo, embeddingRepository: embeddingRepo, provider: provider);

        await step.ExecuteAsync(MakeDocument());

        Assert.DoesNotContain("super-sensitive-content",
            embeddingRepo.Saved[0].FailureReason ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────────────────────────
    // Audit events
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_AllChunksSucceed_EmitsEmbeddingGenerationSucceeded()
    {
        var chunkRepo = new FixedChunkRepository([MakeChunkRecord()]);
        var audit = new RecordingAuditWriter();
        var step = BuildStep(chunkRepository: chunkRepo, audit: audit);

        await step.ExecuteAsync(MakeDocument());

        Assert.Contains(audit.Events, e => e.EventType == AuditEventTypes.EmbeddingGenerationSucceeded);
        Assert.DoesNotContain(audit.Events, e => e.EventType == AuditEventTypes.EmbeddingGenerationFailed);
    }

    [Fact]
    public async Task ExecuteAsync_AnyChunkFails_EmitsEmbeddingGenerationFailed()
    {
        var chunkRepo = new FixedChunkRepository([MakeChunkRecord()]);
        var audit = new RecordingAuditWriter();
        var provider = new FailingEmbeddingProvider("err");
        var step = BuildStep(chunkRepository: chunkRepo, audit: audit, provider: provider);

        await step.ExecuteAsync(MakeDocument());

        Assert.Contains(audit.Events, e => e.EventType == AuditEventTypes.EmbeddingGenerationFailed);
        Assert.DoesNotContain(audit.Events, e => e.EventType == AuditEventTypes.EmbeddingGenerationSucceeded);
    }

    [Fact]
    public async Task ExecuteAsync_AuditFailure_DoesNotPropagateException()
    {
        var chunkRepo = new FixedChunkRepository([MakeChunkRecord()]);
        var step = BuildStep(chunkRepository: chunkRepo, audit: new ThrowingAuditWriter());

        // Must not throw even when audit writer blows up.
        await step.ExecuteAsync(MakeDocument());
    }

    // ──────────────────────────────────────────────────────────────
    // No chunks
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_NoChunks_SavesNoEmbeddings()
    {
        var chunkRepo = new FixedChunkRepository([]);
        var embeddingRepo = new CapturingEmbeddingRepository();
        var step = BuildStep(chunkRepository: chunkRepo, embeddingRepository: embeddingRepo);

        await step.ExecuteAsync(MakeDocument());

        Assert.Empty(embeddingRepo.Saved);
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static GenerateChunkEmbeddingsProcessingStep BuildStep(
        FixedChunkRepository? chunkRepository = null,
        IEmbeddingProvider? provider = null,
        CapturingEmbeddingRepository? embeddingRepository = null,
        IAuditEventWriter? audit = null) =>
        new(
            chunkRepository ?? new FixedChunkRepository([MakeChunkRecord()]),
            provider ?? new FixedEmbeddingProvider(),
            embeddingRepository ?? new CapturingEmbeddingRepository(),
            audit ?? new RecordingAuditWriter(),
            new StubCorrelationContext(),
            NullLogger<GenerateChunkEmbeddingsProcessingStep>.Instance);

    private static ManagedDocument MakeDocument() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "policy.txt",
            "Test Document",
            "text/plain",
            42,
            "local://test/policy.txt",
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

    private static DocumentChunkRecord MakeChunkRecord(
        Guid? documentId = null,
        Guid? organizationId = null,
        int chunkIndex = 0,
        string text = "Hello world.") =>
        new(
            Guid.NewGuid(),
            documentId ?? Guid.NewGuid(),
            organizationId ?? Guid.NewGuid(),
            chunkIndex,
            text,
            text.Length,
            3,
            DateTimeOffset.UtcNow);

    // ──────────────────────────────────────────────────────────────
    // Fakes
    // ──────────────────────────────────────────────────────────────

    private sealed class FixedChunkRepository(IReadOnlyList<DocumentChunkRecord> chunks) : IDocumentChunkRepository
    {
        public Task SaveChunksAsync(IReadOnlyList<DocumentChunkRecord> c, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<DocumentChunkRecord>> GetChunksForDocumentAsync(Guid documentId, CancellationToken ct = default) =>
            Task.FromResult(chunks);
    }

    private sealed class CapturingEmbeddingRepository : IChunkEmbeddingRepository
    {
        public List<ChunkEmbeddingRecord> Saved { get; } = [];

        public Task SaveEmbeddingsAsync(IReadOnlyList<ChunkEmbeddingRecord> embeddings, CancellationToken ct = default)
        {
            Saved.AddRange(embeddings);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedEmbeddingProvider(int dimensions = 4, float[]? vector = null) : IEmbeddingProvider
    {
        public string ProviderName => "Test";
        public string DefaultModelName => "test-model";
        public int DefaultDimensions => dimensions;

        public Task<EmbeddingResponse> GenerateAsync(EmbeddingRequest request, CancellationToken ct = default)
        {
            if (vector is not null)
                return Task.FromResult(new EmbeddingResponse(vector));
            var d = request.Dimensions > 0 ? request.Dimensions : dimensions;
            var v = Enumerable.Range(0, d).Select(i => 0.1f * (i + 1)).ToArray();
            return Task.FromResult(new EmbeddingResponse(v));
        }
    }

    private sealed class FailingEmbeddingProvider(string message) : IEmbeddingProvider
    {
        public string ProviderName => "Test";
        public string DefaultModelName => "test-model";
        public int DefaultDimensions => 4;

        public Task<EmbeddingResponse> GenerateAsync(EmbeddingRequest request, CancellationToken ct = default) =>
            throw new InvalidOperationException(message);
    }

    private sealed class CountingFailingProvider(int failOnCallNumber) : IEmbeddingProvider
    {
        private int _callCount;

        public string ProviderName => "Test";
        public string DefaultModelName => "test-model";
        public int DefaultDimensions => 4;

        public Task<EmbeddingResponse> GenerateAsync(EmbeddingRequest request, CancellationToken ct = default)
        {
            _callCount++;
            if (_callCount == failOnCallNumber)
                throw new InvalidOperationException("Simulated failure.");
            return Task.FromResult(new EmbeddingResponse([0.1f, 0.2f, 0.3f, 0.4f]));
        }
    }

    private sealed class RecordingAuditWriter : IAuditEventWriter
    {
        public List<AuditEvent> Events { get; } = [];

        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default) =>
            throw new InvalidOperationException("Simulated audit failure.");
    }

    private sealed class StubCorrelationContext : ICorrelationContext
    {
        public string CorrelationId => "test-embedding-correlation";
    }
}

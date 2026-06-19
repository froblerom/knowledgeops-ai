using System.Text.Json;
using KnowledgeOps.Application.Retrieval;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using KnowledgeOps.Infrastructure;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeOps.IntegrationTests;

public sealed class LocalVectorStoreSqlTests
{
    [SqlServerFact]
    public async Task LocalVectorStore_IndexesReadyEmbeddings()
    {
        await RunInDatabaseAsync(async (_, context, retrievalIndex, _, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            var seeded = await SeedCandidateAsync(context, orgId, userId, [1, 0]);

            var result = await retrievalIndex.IndexAsync(new VectorIndexRequest(orgId));
            var stored = await context.ChunkEmbeddings.SingleAsync(e => e.Id == seeded.EmbeddingId);

            Assert.Equal(1, result.IndexedCount);
            Assert.Equal(EmbeddingIndexStatus.Indexed, stored.IndexStatus);
            Assert.NotNull(stored.IndexedAt);
            Assert.Null(stored.IndexFailureReason);
        });
    }

    // ── Bug regression: indexing must work during Worker processing ──────────────

    [SqlServerFact]
    public async Task LocalVectorStore_IndexesReadyEmbeddingsWhileDocumentIsProcessing()
    {
        // This test covers the root cause: IndexAsync previously required
        // ProcessingStatus == Processed, so it found 0 eligible embeddings during
        // the Worker's indexing step (which runs before MarkProcessedAsync).
        await RunInDatabaseAsync(async (_, context, retrievalIndex, _, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            var seeded = await SeedCandidateAsync(
                context, orgId, userId, [1, 0],
                processingStatus: "Processing",   // document still in Processing
                retrievalEnabled: false);          // retrieval disabled (default)

            var result = await retrievalIndex.IndexAsync(
                new VectorIndexRequest(orgId, seeded.DocumentId));
            var stored = await context.ChunkEmbeddings.SingleAsync(e => e.Id == seeded.EmbeddingId);

            Assert.Equal(1, result.EligibleEmbeddingCount);
            Assert.Equal(1, result.IndexedCount);
            Assert.Equal(0, result.FailedCount);
            Assert.Equal(EmbeddingIndexStatus.Indexed, stored.IndexStatus);
            Assert.NotNull(stored.IndexedAt);
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_IndexesReadyEmbeddingsWhenRetrievalIsDisabled()
    {
        // This test covers the second root cause: IndexAsync previously required
        // IsRetrievalEnabled == true. Newly uploaded documents have it false by default.
        await RunInDatabaseAsync(async (_, context, retrievalIndex, _, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            var seeded = await SeedCandidateAsync(
                context, orgId, userId, [1, 0],
                processingStatus: "Processed",
                retrievalEnabled: false);          // retrieval not yet enabled

            var result = await retrievalIndex.IndexAsync(new VectorIndexRequest(orgId));
            var stored = await context.ChunkEmbeddings.SingleAsync(e => e.Id == seeded.EmbeddingId);

            Assert.Equal(1, result.IndexedCount);
            Assert.Equal(EmbeddingIndexStatus.Indexed, stored.IndexStatus);
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_SearchStillExcludesRetrievalDisabledDocuments()
    {
        // Regression guard: fixing IndexAsync must not weaken SearchAsync eligibility.
        await RunInDatabaseAsync(async (_, context, _, searchProvider, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            var included = await SeedCandidateAsync(
                context, orgId, userId, [1, 0],
                processingStatus: "Processed", retrievalEnabled: true,
                indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(
                context, orgId, userId, [1, 0],
                processingStatus: "Processed", retrievalEnabled: false,
                indexStatus: EmbeddingIndexStatus.Indexed);

            var result = await searchProvider.SearchAsync(
                new SemanticQueryRequest(orgId, [1, 0], TopK: 10));

            var candidate = Assert.Single(result.Candidates);
            Assert.Equal(included.EmbeddingId, candidate.ChunkEmbeddingId);
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_SearchStillExcludesProcessingDocuments()
    {
        // Regression guard: SearchAsync must still exclude non-Processed documents.
        await RunInDatabaseAsync(async (_, context, _, searchProvider, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            var included = await SeedCandidateAsync(
                context, orgId, userId, [1, 0],
                processingStatus: "Processed", retrievalEnabled: true,
                indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(
                context, orgId, userId, [1, 0],
                processingStatus: "Processing", retrievalEnabled: true,
                indexStatus: EmbeddingIndexStatus.Indexed);

            var result = await searchProvider.SearchAsync(
                new SemanticQueryRequest(orgId, [1, 0], TopK: 10));

            var candidate = Assert.Single(result.Candidates);
            Assert.Equal(included.EmbeddingId, candidate.ChunkEmbeddingId);
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_DoesNotIndexFailedEmbeddings()
    {
        await RunInDatabaseAsync(async (_, context, retrievalIndex, _, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            var seeded = await SeedCandidateAsync(
                context,
                orgId,
                userId,
                [1, 0],
                embeddingStatus: EmbeddingStatus.Failed);

            var result = await retrievalIndex.IndexAsync(new VectorIndexRequest(orgId));
            var stored = await context.ChunkEmbeddings.SingleAsync(e => e.Id == seeded.EmbeddingId);

            Assert.Equal(0, result.IndexedCount);
            Assert.Null(stored.IndexStatus);
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_MarksIndexFailureWithSafeReason()
    {
        await RunInDatabaseAsync(async (_, context, retrievalIndex, _, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            var seeded = await SeedCandidateAsync(context, orgId, userId, vectorJson: "[not-valid-vector-json]");

            var result = await retrievalIndex.IndexAsync(new VectorIndexRequest(orgId));
            var stored = await context.ChunkEmbeddings.SingleAsync(e => e.Id == seeded.EmbeddingId);

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(EmbeddingIndexStatus.Failed, stored.IndexStatus);
            Assert.Equal("Embedding vector data was invalid.", stored.IndexFailureReason);
            Assert.DoesNotContain("not-valid-vector-json", stored.IndexFailureReason, StringComparison.Ordinal);
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_MarksIndexFailureForEmptyDimensionMismatchAndZeroNormVectors()
    {
        await RunInDatabaseAsync(async (_, context, retrievalIndex, _, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            await SeedCandidateAsync(context, orgId, userId, vectorJson: "[]");
            await SeedCandidateAsync(context, orgId, userId, [1, 0], vectorDimensionsOverride: 3);
            await SeedCandidateAsync(context, orgId, userId, [0, 0]);

            var result = await retrievalIndex.IndexAsync(new VectorIndexRequest(orgId));
            var failed = await context.ChunkEmbeddings
                .Where(e => e.IndexStatus == EmbeddingIndexStatus.Failed)
                .ToListAsync();

            Assert.Equal(3, result.FailedCount);
            Assert.Equal(3, failed.Count);
            Assert.All(failed, embedding =>
                Assert.Equal("Embedding vector data was invalid.", embedding.IndexFailureReason));
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_SearchReturnsTopKByCosineSimilarity()
    {
        await RunInDatabaseAsync(async (_, context, _, searchProvider, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            var best = await SeedCandidateAsync(context, orgId, userId, [1, 0], chunkIndex: 0, indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, [0.8f, 0.2f], chunkIndex: 1, indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, [0, 1], chunkIndex: 2, indexStatus: EmbeddingIndexStatus.Indexed);

            var result = await searchProvider.SearchAsync(new SemanticQueryRequest(orgId, [1, 0], TopK: 2));

            Assert.Equal(2, result.Candidates.Count);
            Assert.Equal(best.EmbeddingId, result.Candidates[0].ChunkEmbeddingId);
            Assert.Equal("CosineSimilarity", result.ScoreMethod);
            Assert.Equal(2, result.EffectiveTopK);
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_SearchReturnsDeterministicRanking()
    {
        await RunInDatabaseAsync(async (_, context, _, searchProvider, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            await SeedCandidateAsync(context, orgId, userId, [1, 0], chunkIndex: 2, indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, [1, 0], chunkIndex: 1, indexStatus: EmbeddingIndexStatus.Indexed);

            var result = await searchProvider.SearchAsync(new SemanticQueryRequest(orgId, [1, 0], TopK: 2));

            Assert.Equal([1, 2], result.Candidates.Select(candidate => candidate.ChunkIndex).ToArray());
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_EnforcesMaxTopK()
    {
        await RunInDatabaseAsync(async (_, context, _, searchProvider, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            await SeedCandidateAsync(context, orgId, userId, [1, 0], chunkIndex: 0, indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, [0.9f, 0.1f], chunkIndex: 1, indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, [0.8f, 0.2f], chunkIndex: 2, indexStatus: EmbeddingIndexStatus.Indexed);

            var result = await searchProvider.SearchAsync(new SemanticQueryRequest(orgId, [1, 0], TopK: 99));

            Assert.Equal(2, result.EffectiveTopK);
            Assert.Equal(2, result.Candidates.Count);
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_UsesDefaultTopKWhenTopKIsZeroOrNegative()
    {
        await RunInDatabaseAsync(async (_, context, _, searchProvider, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            await SeedCandidateAsync(context, orgId, userId, [1, 0], chunkIndex: 0, indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, [0, 1], chunkIndex: 1, indexStatus: EmbeddingIndexStatus.Indexed);

            var zero = await searchProvider.SearchAsync(new SemanticQueryRequest(orgId, [1, 0], TopK: 0));
            var negative = await searchProvider.SearchAsync(new SemanticQueryRequest(orgId, [1, 0], TopK: -5));

            Assert.Equal(1, zero.EffectiveTopK);
            Assert.Single(zero.Candidates);
            Assert.Equal(1, negative.EffectiveTopK);
            Assert.Single(negative.Candidates);
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_SearchReturnsDocumentChunkOrganizationTraceability()
    {
        await RunInDatabaseAsync(async (_, context, _, searchProvider, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            var seeded = await SeedCandidateAsync(
                context,
                orgId,
                userId,
                [1, 0],
                chunkIndex: 7,
                pageNumber: 3,
                sectionLabel: "Trace",
                indexStatus: EmbeddingIndexStatus.Indexed);

            var result = await searchProvider.SearchAsync(new SemanticQueryRequest(orgId, [1, 0], TopK: 1));
            var candidate = Assert.Single(result.Candidates);

            Assert.Equal(orgId, candidate.OrganizationId);
            Assert.Equal(seeded.DocumentId, candidate.DocumentId);
            Assert.Equal(seeded.ChunkId, candidate.ChunkId);
            Assert.Equal(seeded.EmbeddingId, candidate.ChunkEmbeddingId);
            Assert.Equal("Fake", candidate.ProviderName);
            Assert.Equal("fake-deterministic-v1", candidate.ModelName);
            Assert.Equal(7, candidate.ChunkIndex);
            Assert.Equal(3, candidate.PageNumber);
            Assert.Equal("Trace", candidate.SectionLabel);
            Assert.Equal("CosineSimilarity", candidate.RetrievalScore.Method);
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_SearchFiltersByOrganizationBeforeScoring()
    {
        await RunInDatabaseAsync(async (_, context, _, searchProvider, _) =>
        {
            var (orgAId, userAId) = await SeedOrganizationAndUserAsync(context, "Org A");
            var (orgBId, userBId) = await SeedOrganizationAndUserAsync(context, "Org B");
            var orgA = await SeedCandidateAsync(context, orgAId, userAId, [0, 1], indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgBId, userBId, [1, 0], indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgBId, userBId, vectorJson: "not-json", indexStatus: EmbeddingIndexStatus.Indexed);

            var result = await searchProvider.SearchAsync(new SemanticQueryRequest(orgAId, [1, 0], TopK: 10));

            var candidate = Assert.Single(result.Candidates);
            Assert.Equal(orgA.EmbeddingId, candidate.ChunkEmbeddingId);
            Assert.Equal(orgAId, candidate.OrganizationId);
            Assert.Equal(0, result.ExcludedMalformedVectorCount);
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_SearchExcludesIneligibleCandidates()
    {
        await RunInDatabaseAsync(async (_, context, _, searchProvider, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            var included = await SeedCandidateAsync(context, orgId, userId, [1, 0], indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedDocumentAndChunkWithoutEmbeddingAsync(context, orgId, userId);
            await SeedCandidateAsync(context, orgId, userId, [1, 0], embeddingStatus: EmbeddingStatus.Failed, indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, [1, 0], indexStatus: null);
            await SeedCandidateAsync(context, orgId, userId, [1, 0], indexStatus: EmbeddingIndexStatus.Failed);
            await SeedCandidateAsync(context, orgId, userId, [1, 0], retrievalEnabled: false, indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, [1, 0], documentDeleted: true, indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, [1, 0], processingStatus: "Processing", indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, [1, 0], chunkDeleted: true, indexStatus: EmbeddingIndexStatus.Indexed);

            var result = await searchProvider.SearchAsync(new SemanticQueryRequest(orgId, [1, 0], TopK: 10));

            var candidate = Assert.Single(result.Candidates);
            Assert.Equal(included.EmbeddingId, candidate.ChunkEmbeddingId);
        });
    }

    [SqlServerFact]
    public async Task LocalVectorStore_HandlesMalformedEmptyDimensionMismatchAndZeroNormSafely()
    {
        await RunInDatabaseAsync(async (_, context, _, searchProvider, _) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            var included = await SeedCandidateAsync(context, orgId, userId, [1, 0], indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, vectorJson: "not-json", indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, vectorJson: "[]", indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, [1, 0, 0], indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, [0, 0], indexStatus: EmbeddingIndexStatus.Indexed);

            var result = await searchProvider.SearchAsync(new SemanticQueryRequest(orgId, [1, 0], TopK: 10));

            var candidate = Assert.Single(result.Candidates);
            Assert.Equal(included.EmbeddingId, candidate.ChunkEmbeddingId);
            Assert.Equal(2, result.ExcludedMalformedVectorCount);
            Assert.Equal(1, result.ExcludedDimensionMismatchCount);
            Assert.Equal(1, result.ExcludedZeroNormVectorCount);
        });
    }

    [SqlServerFact]
    public async Task RetrievalHealth_ReturnsIndexedAndFailedCounts()
    {
        await RunInDatabaseAsync(async (_, context, _, _, healthCheck) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context);
            await SeedCandidateAsync(context, orgId, userId, [1, 0], indexStatus: EmbeddingIndexStatus.Indexed);
            await SeedCandidateAsync(context, orgId, userId, [1, 0], indexStatus: EmbeddingIndexStatus.Failed);

            var result = await healthCheck.CheckAsync();

            Assert.True(result.IsHealthy);
            Assert.Equal("LocalSqlVectorStore", result.ProviderName);
            Assert.Equal(1, result.IndexedEmbeddingCount);
            Assert.Equal(1, result.FailedIndexCount);
            Assert.Null(result.DegradedReason);
        });
    }

    [Fact]
    public async Task RetrievalHealth_DoesNotExposeSensitiveDataWhenStorageUnavailable()
    {
        var options = new DbContextOptionsBuilder<KnowledgeOpsDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=DisposedRetrievalHealth;Trusted_Connection=True;")
            .Options;
        await using var context = new KnowledgeOpsDbContext(options);
        await context.DisposeAsync();

        var healthCheck = new KnowledgeOps.Infrastructure.Retrieval.LocalRetrievalStorageHealthCheck(context);

        var result = await healthCheck.CheckAsync();

        Assert.False(result.IsHealthy);
        Assert.Equal("LocalSqlVectorStore", result.ProviderName);
        Assert.Equal("Retrieval storage is unavailable.", result.DegradedReason);
        Assert.DoesNotContain("Server=", result.DegradedReason, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DisposedRetrievalHealth", result.DegradedReason, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task RunInDatabaseAsync(
        Func<ServiceProvider, KnowledgeOpsDbContext, IRetrievalIndex, ISemanticSearchProvider, IRetrievalStorageHealthCheck, Task> test)
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue28Retrieval_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = databaseConnectionString,
                    ["Retrieval:DefaultTopK"] = "1",
                    ["Retrieval:MaxTopK"] = "2"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);

            var provider = services.BuildServiceProvider();
            await using var _ = provider;
            var context = provider.GetRequiredService<KnowledgeOpsDbContext>();
            await context.Database.MigrateAsync();

            await test(
                provider,
                context,
                provider.GetRequiredService<IRetrievalIndex>(),
                provider.GetRequiredService<ISemanticSearchProvider>(),
                provider.GetRequiredService<IRetrievalStorageHealthCheck>());
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    private static async Task<(Guid orgId, Guid userId)> SeedOrganizationAndUserAsync(
        KnowledgeOpsDbContext context,
        string organizationName = "Retrieval Org")
    {
        var timestamp = DateTimeOffset.UtcNow;
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = $"{organizationName} {Guid.NewGuid():N}",
            Status = OrganizationStatus.Active,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            DisplayName = $"retrieval-{Guid.NewGuid():N}@example.test",
            Email = $"retrieval-{Guid.NewGuid():N}@example.test",
            Status = UserStatus.Active,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        context.AddRange(org, user);
        await context.SaveChangesAsync();
        return (org.Id, user.Id);
    }

    private static async Task<SeededCandidate> SeedCandidateAsync(
        KnowledgeOpsDbContext context,
        Guid organizationId,
        Guid userId,
        IReadOnlyList<float>? vector = null,
        string? vectorJson = null,
        int chunkIndex = 0,
        int? pageNumber = null,
        string? sectionLabel = null,
        string processingStatus = "Processed",
        bool retrievalEnabled = true,
        bool documentDeleted = false,
        bool chunkDeleted = false,
        EmbeddingStatus embeddingStatus = EmbeddingStatus.Ready,
        EmbeddingIndexStatus? indexStatus = null,
        int? vectorDimensionsOverride = null)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var documentId = Guid.NewGuid();
        await InsertDocumentAsync(
            context,
            documentId,
            organizationId,
            userId,
            $"retrieval-{Guid.NewGuid():N}.txt",
            timestamp,
            processingStatus,
            retrievalEnabled,
            documentDeleted ? timestamp : null);

        var chunkId = Guid.NewGuid();
        context.DocumentChunks.Add(new DocumentChunk
        {
            Id = chunkId,
            DocumentId = documentId,
            OrganizationId = organizationId,
            ChunkIndex = chunkIndex,
            Text = "Sensitive chunk text must not be returned.",
            PageNumber = pageNumber,
            SectionLabel = sectionLabel,
            CharacterLength = 42,
            TokenEstimate = 11,
            CreatedAt = timestamp,
            DeletedAt = chunkDeleted ? timestamp : null
        });

        var serializedVector = vectorJson ?? JsonSerializer.Serialize(vector ?? [1, 0]);
        var embeddingId = Guid.NewGuid();
        context.ChunkEmbeddings.Add(new ChunkEmbedding
        {
            Id = embeddingId,
            ChunkId = chunkId,
            OrganizationId = organizationId,
            ProviderName = "Fake",
            ModelName = "fake-deterministic-v1",
            VectorData = serializedVector,
            VectorDimensions = vectorDimensionsOverride ?? vector?.Count,
            Status = embeddingStatus,
            FailureReason = embeddingStatus == EmbeddingStatus.Failed ? "Embedding vector was invalid." : null,
            IndexStatus = indexStatus,
            IndexedAt = indexStatus == EmbeddingIndexStatus.Indexed ? timestamp : null,
            IndexFailureReason = indexStatus == EmbeddingIndexStatus.Failed ? "Embedding vector data was invalid." : null,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        });

        await context.SaveChangesAsync();
        return new SeededCandidate(documentId, chunkId, embeddingId);
    }

    private static async Task SeedDocumentAndChunkWithoutEmbeddingAsync(
        KnowledgeOpsDbContext context,
        Guid organizationId,
        Guid userId)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var documentId = Guid.NewGuid();
        await InsertDocumentAsync(
            context,
            documentId,
            organizationId,
            userId,
            $"missing-embedding-{Guid.NewGuid():N}.txt",
            timestamp,
            "Processed",
            retrievalEnabled: true,
            deletedAt: null);

        context.DocumentChunks.Add(new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            OrganizationId = organizationId,
            ChunkIndex = 50,
            Text = "Chunk without embedding.",
            CreatedAt = timestamp
        });
        await context.SaveChangesAsync();
    }

    private static Task InsertDocumentAsync(
        KnowledgeOpsDbContext context,
        Guid documentId,
        Guid organizationId,
        Guid uploadedByUserId,
        string fileName,
        DateTimeOffset uploadedAt,
        string processingStatus,
        bool retrievalEnabled,
        DateTimeOffset? deletedAt) =>
        context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [documents] (
                [document_id], [organization_id], [uploaded_by_user_id], [file_name], [title],
                [content_type], [file_size_bytes], [storage_location], [processing_status],
                [is_retrieval_enabled], [uploaded_at], [processing_started_at], [processed_at],
                [created_at], [updated_at], [deleted_at])
            VALUES (
                {documentId}, {organizationId}, {uploadedByUserId}, {fileName}, {fileName},
                {"text/plain"}, {42L}, {"local://test/" + fileName}, {processingStatus},
                {retrievalEnabled}, {uploadedAt.UtcDateTime}, {uploadedAt.UtcDateTime},
                {(processingStatus == "Processed" ? uploadedAt.UtcDateTime : (DateTime?)null)},
                {uploadedAt.UtcDateTime}, {uploadedAt.UtcDateTime},
                {(deletedAt.HasValue ? deletedAt.Value.UtcDateTime : (DateTime?)null)});
            """);

    private static string WithDatabase(string connectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = databaseName };
        return builder.ConnectionString;
    }

    private static async Task DropDatabaseAsync(string connectionString, string databaseName)
    {
        var masterConnectionString = WithDatabase(connectionString, "master");
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{databaseName}];
            END
            """;
        await command.ExecuteNonQueryAsync();
    }

    private sealed record SeededCandidate(Guid DocumentId, Guid ChunkId, Guid EmbeddingId);
}

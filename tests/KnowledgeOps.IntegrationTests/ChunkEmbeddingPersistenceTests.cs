using KnowledgeOps.Application.Embeddings;
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

public sealed class ChunkEmbeddingPersistenceTests
{
    [SqlServerFact]
    public async Task SaveEmbeddingsAsync_PersistsReadyEmbeddingWithVectorData()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue27SaveEmbeddings_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, embeddingRepository) = await BuildAndMigrateAsync(databaseConnectionString);
            await using var _ = services;

            var (orgId, docId, chunkId) = await SeedDocumentAndChunkAsync(context);

            var now = DateTimeOffset.UtcNow;
            var record = new ChunkEmbeddingRecord(
                EmbeddingId: Guid.NewGuid(),
                ChunkId: chunkId,
                OrganizationId: orgId,
                ProviderName: "Fake",
                ModelName: "fake-deterministic-v1",
                VectorData: "[0.1,0.2,0.3]",
                VectorDimensions: 3,
                Status: EmbeddingStatus.Ready,
                FailureReason: null,
                CreatedAt: now,
                UpdatedAt: now);

            await embeddingRepository.SaveEmbeddingsAsync([record]);

            var stored = await context.ChunkEmbeddings
                .AsNoTracking()
                .SingleAsync(e => e.ChunkId == chunkId);

            Assert.Equal("Fake", stored.ProviderName);
            Assert.Equal("fake-deterministic-v1", stored.ModelName);
            Assert.Equal("[0.1,0.2,0.3]", stored.VectorData);
            Assert.Equal(3, stored.VectorDimensions);
            Assert.Equal(EmbeddingStatus.Ready, stored.Status);
            Assert.Null(stored.FailureReason);
            Assert.Equal(orgId, stored.OrganizationId);
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    [SqlServerFact]
    public async Task SaveEmbeddingsAsync_PersistsFailedEmbeddingWithReason()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue27SaveFailed_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, embeddingRepository) = await BuildAndMigrateAsync(databaseConnectionString);
            await using var _ = services;

            var (orgId, docId, chunkId) = await SeedDocumentAndChunkAsync(context);

            var now = DateTimeOffset.UtcNow;
            var record = new ChunkEmbeddingRecord(
                EmbeddingId: Guid.NewGuid(),
                ChunkId: chunkId,
                OrganizationId: orgId,
                ProviderName: "Fake",
                ModelName: "fake-deterministic-v1",
                VectorData: null,
                VectorDimensions: null,
                Status: EmbeddingStatus.Failed,
                FailureReason: "Embedding vector was invalid.",
                CreatedAt: now,
                UpdatedAt: now);

            await embeddingRepository.SaveEmbeddingsAsync([record]);

            var stored = await context.ChunkEmbeddings
                .AsNoTracking()
                .SingleAsync(e => e.ChunkId == chunkId);

            Assert.Equal(EmbeddingStatus.Failed, stored.Status);
            Assert.Equal("Embedding vector was invalid.", stored.FailureReason);
            Assert.Null(stored.VectorData);
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    [SqlServerFact]
    public async Task SaveEmbeddingsAsync_UniqueConstraint_PreventsDuplicateChunkEmbedding()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue27UniqueEmbedding_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, embeddingRepository) = await BuildAndMigrateAsync(databaseConnectionString);
            await using var _ = services;

            var (orgId, docId, chunkId) = await SeedDocumentAndChunkAsync(context);

            var now = DateTimeOffset.UtcNow;
            var first = new ChunkEmbeddingRecord(
                Guid.NewGuid(), chunkId, orgId, "Fake", "fake-model", "[0.1]", 1, EmbeddingStatus.Ready, null, now, now);

            await embeddingRepository.SaveEmbeddingsAsync([first]);

            var duplicate = new ChunkEmbeddingRecord(
                Guid.NewGuid(), chunkId, orgId, "Fake", "fake-model", "[0.2]", 1, EmbeddingStatus.Ready, null, now, now);

            await Assert.ThrowsAnyAsync<Exception>(() => embeddingRepository.SaveEmbeddingsAsync([duplicate]));
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    [SqlServerFact]
    public async Task GetChunksForDocumentAsync_ReturnsOnlyNonDeletedChunksInOrder()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue27GetChunks_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, _, chunkRepository) = await BuildAndMigrateWithChunkRepoAsync(databaseConnectionString);
            await using var _ = services;

            var (orgId, docId, _) = await SeedDocumentAndChunkAsync(context);

            // Add a second chunk
            var now = DateTimeOffset.UtcNow;
            var secondChunkId = Guid.NewGuid();
            context.DocumentChunks.Add(new DocumentChunk
            {
                Id = secondChunkId,
                DocumentId = docId,
                OrganizationId = orgId,
                ChunkIndex = 1,
                Text = "Second chunk.",
                CharacterLength = 13,
                TokenEstimate = 4,
                CreatedAt = now
            });

            // Add a soft-deleted chunk — should not appear
            var deletedChunkId = Guid.NewGuid();
            context.DocumentChunks.Add(new DocumentChunk
            {
                Id = deletedChunkId,
                DocumentId = docId,
                OrganizationId = orgId,
                ChunkIndex = 2,
                Text = "Deleted chunk.",
                CharacterLength = 14,
                TokenEstimate = 4,
                CreatedAt = now,
                DeletedAt = now
            });

            await context.SaveChangesAsync();

            var chunks = await chunkRepository.GetChunksForDocumentAsync(docId);

            Assert.Equal(2, chunks.Count);
            Assert.Equal(0, chunks[0].ChunkIndex);
            Assert.Equal(1, chunks[1].ChunkIndex);
            Assert.DoesNotContain(chunks, c => c.ChunkId == deletedChunkId);
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static async Task<(ServiceProvider services, KnowledgeOpsDbContext context, IChunkEmbeddingRepository embeddingRepository)>
        BuildAndMigrateAsync(string connectionString)
    {
        var (provider, context, embeddingRepo, _) = await BuildAndMigrateWithChunkRepoAsync(connectionString);
        return (provider, context, embeddingRepo);
    }

    private static async Task<(ServiceProvider services, KnowledgeOpsDbContext context, IChunkEmbeddingRepository embeddingRepository, KnowledgeOps.Application.Documents.IDocumentChunkRepository chunkRepository)>
        BuildAndMigrateWithChunkRepoAsync(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KnowledgeOpsDbContext>();
        var embeddingRepo = scope.ServiceProvider.GetRequiredService<IChunkEmbeddingRepository>();
        var chunkRepo = scope.ServiceProvider.GetRequiredService<KnowledgeOps.Application.Documents.IDocumentChunkRepository>();
        await context.Database.MigrateAsync();

        return (provider, context, embeddingRepo, chunkRepo);
    }

    private static async Task<(Guid orgId, Guid docId, Guid chunkId)> SeedDocumentAndChunkAsync(
        KnowledgeOpsDbContext context)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Embedding Test Org",
            Status = OrganizationStatus.Active,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            DisplayName = $"user-{Guid.NewGuid():N}@example.test",
            Email = $"user-{Guid.NewGuid():N}@example.test",
            Status = UserStatus.Active,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
        context.AddRange(org, user);
        await context.SaveChangesAsync();

        var docId = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [documents] (
                [document_id], [organization_id], [uploaded_by_user_id], [file_name], [title],
                [content_type], [file_size_bytes], [storage_location], [processing_status],
                [uploaded_at], [created_at], [updated_at], [deleted_at])
            VALUES (
                {docId}, {org.Id}, {user.Id}, N'test.txt', N'Test Document',
                N'text/plain', {42L}, N'local://test/test.txt', N'Processing',
                {timestamp.UtcDateTime}, {timestamp.UtcDateTime}, {timestamp.UtcDateTime}, {(DateTime?)null});
            """);

        var chunkId = Guid.NewGuid();
        context.DocumentChunks.Add(new DocumentChunk
        {
            Id = chunkId,
            DocumentId = docId,
            OrganizationId = org.Id,
            ChunkIndex = 0,
            Text = "First chunk text.",
            CharacterLength = 17,
            TokenEstimate = 5,
            CreatedAt = timestamp
        });
        await context.SaveChangesAsync();

        return (org.Id, docId, chunkId);
    }

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
}

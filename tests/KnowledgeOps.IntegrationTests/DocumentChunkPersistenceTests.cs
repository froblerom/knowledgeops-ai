using KnowledgeOps.Application.Documents;
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

public sealed class DocumentChunkPersistenceTests
{
    [SqlServerFact]
    public async Task SaveChunksAsync_PersistsAllChunksWithCorrectData()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue23SaveChunks_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, chunkRepository) = await BuildAndMigrateAsync(databaseConnectionString);
            await using var _ = services;

            var timestamp = DateTimeOffset.UtcNow;
            var org = CreateOrganization("Chunk Org", timestamp);
            var user = CreateUser(org.Id, $"chunk-{Guid.NewGuid():N}@example.test", timestamp);
            context.AddRange(org, user);
            await context.SaveChangesAsync();

            var docId = Guid.NewGuid();
            await InsertDocumentAsync(context, docId, org.Id, user.Id, "chunk.txt", timestamp, "Processing", null);

            var now = DateTimeOffset.UtcNow;
            var chunks = new[]
            {
                new DocumentChunkRecord(Guid.NewGuid(), docId, org.Id, 0, "First chunk text.", 17, 5, now),
                new DocumentChunkRecord(Guid.NewGuid(), docId, org.Id, 1, "Second chunk text.", 18, 5, now)
            };

            await chunkRepository.SaveChunksAsync(chunks);

            var stored = await context.DocumentChunks
                .AsNoTracking()
                .Where(c => c.DocumentId == docId)
                .OrderBy(c => c.ChunkIndex)
                .ToListAsync();

            Assert.Equal(2, stored.Count);
            Assert.Equal(0, stored[0].ChunkIndex);
            Assert.Equal("First chunk text.", stored[0].Text);
            Assert.Equal(17, stored[0].CharacterLength);
            Assert.Equal(5, stored[0].TokenEstimate);
            Assert.Equal(org.Id, stored[0].OrganizationId);
            Assert.Equal(1, stored[1].ChunkIndex);
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    [SqlServerFact]
    public async Task SaveChunksAsync_UniqueConstraint_PreventsDuplicateChunkIndex()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue23UniqueChunk_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, chunkRepository) = await BuildAndMigrateAsync(databaseConnectionString);
            await using var _ = services;

            var timestamp = DateTimeOffset.UtcNow;
            var org = CreateOrganization("Unique Org", timestamp);
            var user = CreateUser(org.Id, $"unique-{Guid.NewGuid():N}@example.test", timestamp);
            context.AddRange(org, user);
            await context.SaveChangesAsync();

            var docId = Guid.NewGuid();
            await InsertDocumentAsync(context, docId, org.Id, user.Id, "unique.txt", timestamp, "Processing", null);

            var now = DateTimeOffset.UtcNow;
            var firstChunks = new[]
            {
                new DocumentChunkRecord(Guid.NewGuid(), docId, org.Id, 0, "Chunk zero.", 11, 3, now)
            };
            await chunkRepository.SaveChunksAsync(firstChunks);

            // Saving a duplicate chunk index for the same document must fail.
            var duplicateChunks = new[]
            {
                new DocumentChunkRecord(Guid.NewGuid(), docId, org.Id, 0, "Duplicate zero.", 15, 4, now)
            };
            await Assert.ThrowsAnyAsync<Exception>(() => chunkRepository.SaveChunksAsync(duplicateChunks));
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static async Task<(ServiceProvider services, KnowledgeOpsDbContext context, IDocumentChunkRepository repository)>
        BuildAndMigrateAsync(string connectionString)
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
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentChunkRepository>();
        await context.Database.MigrateAsync();

        return (provider, context, repository);
    }

    private static Task InsertDocumentAsync(
        KnowledgeOpsDbContext context,
        Guid documentId,
        Guid organizationId,
        Guid uploadedByUserId,
        string fileName,
        DateTimeOffset uploadedAt,
        string processingStatus,
        DateTimeOffset? deletedAt) =>
        context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [documents] (
                [document_id], [organization_id], [uploaded_by_user_id], [file_name], [title],
                [content_type], [file_size_bytes], [storage_location], [processing_status],
                [uploaded_at], [created_at], [updated_at], [deleted_at])
            VALUES (
                {documentId}, {organizationId}, {uploadedByUserId}, {fileName}, {fileName},
                {"text/plain"}, {42L}, {"local://test/" + fileName}, {processingStatus},
                {uploadedAt.UtcDateTime}, {uploadedAt.UtcDateTime}, {uploadedAt.UtcDateTime},
                {(deletedAt.HasValue ? deletedAt.Value.UtcDateTime : (DateTime?)null)});
            """);

    private static Organization CreateOrganization(string name, DateTimeOffset timestamp) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Status = OrganizationStatus.Active,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

    private static User CreateUser(Guid organizationId, string email, DateTimeOffset timestamp) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            DisplayName = email,
            Email = email,
            Status = UserStatus.Active,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

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

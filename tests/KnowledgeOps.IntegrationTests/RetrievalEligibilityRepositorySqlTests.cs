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

public sealed class RetrievalEligibilityRepositorySqlTests
{
    [SqlServerFact]
    public async Task RevalidateAsync_ReturnsOnlyCurrentlyEligibleSameOrganizationIdentities()
    {
        await RunInDatabaseAsync(async (_, context, repository) =>
        {
            var (orgId, userId) = await SeedOrganizationAndUserAsync(context, "Eligibility Org");
            var (otherOrgId, otherUserId) = await SeedOrganizationAndUserAsync(context, "Other Org");

            var valid = await SeedCandidateAsync(context, orgId, userId);
            var uploaded = await SeedCandidateAsync(context, orgId, userId, processingStatus: "Uploaded");
            var processing = await SeedCandidateAsync(context, orgId, userId, processingStatus: "Processing");
            var failedDocument = await SeedCandidateAsync(context, orgId, userId, processingStatus: "Failed");
            var retrievalDisabled = await SeedCandidateAsync(context, orgId, userId, retrievalEnabled: false);
            var softDeletedDocument = await SeedCandidateAsync(context, orgId, userId, documentDeleted: true);
            var crossOrgDocument = await SeedCandidateAsync(
                context,
                orgId,
                userId,
                documentOrganizationId: otherOrgId,
                documentUploadedByUserId: otherUserId);
            var crossOrgChunk = await SeedCandidateAsync(context, orgId, userId, chunkOrganizationId: otherOrgId);
            var missingEmbedding = await SeedDocumentAndChunkWithoutEmbeddingAsync(context, orgId, userId);
            var failedEmbedding = await SeedCandidateAsync(context, orgId, userId, embeddingStatus: EmbeddingStatus.Failed);
            var failedIndex = await SeedCandidateAsync(context, orgId, userId, indexStatus: EmbeddingIndexStatus.Failed);
            var unindexed = await SeedCandidateAsync(context, orgId, userId, indexStatus: null);
            var stale = valid with { ChunkEmbeddingId = Guid.NewGuid() };
            var crossOrgCandidate = await SeedCandidateAsync(context, otherOrgId, otherUserId);

            var result = await repository.RevalidateAsync(
                orgId,
                [
                    valid,
                    uploaded,
                    processing,
                    failedDocument,
                    retrievalDisabled,
                    softDeletedDocument,
                    crossOrgDocument,
                    crossOrgChunk,
                    missingEmbedding,
                    failedEmbedding,
                    failedIndex,
                    unindexed,
                    stale,
                    crossOrgCandidate
                ]);

            var identity = Assert.Single(result);
            Assert.Equal(orgId, identity.OrganizationId);
            Assert.Equal(valid.DocumentId, identity.DocumentId);
            Assert.Equal(valid.ChunkId, identity.ChunkId);
            Assert.Equal(valid.ChunkEmbeddingId, identity.ChunkEmbeddingId);
        });
    }

    private static async Task RunInDatabaseAsync(
        Func<ServiceProvider, KnowledgeOpsDbContext, IRetrievalEligibilityRepository, Task> test)
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue29Eligibility_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = databaseConnectionString
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);

            var provider = services.BuildServiceProvider();
            await using var _ = provider;
            var context = provider.GetRequiredService<KnowledgeOpsDbContext>();
            await context.Database.MigrateAsync();

            await test(provider, context, provider.GetRequiredService<IRetrievalEligibilityRepository>());
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    private static async Task<(Guid orgId, Guid userId)> SeedOrganizationAndUserAsync(
        KnowledgeOpsDbContext context,
        string organizationName)
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
            DisplayName = $"eligibility-{Guid.NewGuid():N}@example.test",
            Email = $"eligibility-{Guid.NewGuid():N}@example.test",
            Status = UserStatus.Active,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        context.AddRange(org, user);
        await context.SaveChangesAsync();
        return (org.Id, user.Id);
    }

    private static async Task<RetrievalCandidateIdentity> SeedCandidateAsync(
        KnowledgeOpsDbContext context,
        Guid organizationId,
        Guid userId,
        string processingStatus = "Processed",
        bool retrievalEnabled = true,
        bool documentDeleted = false,
        EmbeddingStatus embeddingStatus = EmbeddingStatus.Ready,
        EmbeddingIndexStatus? indexStatus = EmbeddingIndexStatus.Indexed,
        Guid? documentOrganizationId = null,
        Guid? documentUploadedByUserId = null,
        Guid? chunkOrganizationId = null)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var documentId = Guid.NewGuid();
        await InsertDocumentAsync(
            context,
            documentId,
            documentOrganizationId ?? organizationId,
            documentUploadedByUserId ?? userId,
            $"eligibility-{Guid.NewGuid():N}.txt",
            timestamp,
            processingStatus,
            retrievalEnabled,
            documentDeleted ? timestamp : null);

        var chunkId = Guid.NewGuid();
        context.DocumentChunks.Add(new DocumentChunk
        {
            Id = chunkId,
            DocumentId = documentId,
            OrganizationId = chunkOrganizationId ?? organizationId,
            ChunkIndex = 0,
            Text = "Chunk text must never be returned from eligibility revalidation.",
            CharacterLength = 61,
            TokenEstimate = 12,
            CreatedAt = timestamp
        });

        var embeddingId = Guid.NewGuid();
        context.ChunkEmbeddings.Add(new ChunkEmbedding
        {
            Id = embeddingId,
            ChunkId = chunkId,
            OrganizationId = organizationId,
            ProviderName = "Fake",
            ModelName = "fake-deterministic-v1",
            VectorData = embeddingStatus == EmbeddingStatus.Ready ? "[1,0]" : null,
            VectorDimensions = embeddingStatus == EmbeddingStatus.Ready ? 2 : null,
            Status = embeddingStatus,
            FailureReason = embeddingStatus == EmbeddingStatus.Failed ? "Embedding failed." : null,
            IndexStatus = indexStatus,
            IndexedAt = indexStatus == EmbeddingIndexStatus.Indexed ? timestamp : null,
            IndexFailureReason = indexStatus == EmbeddingIndexStatus.Failed ? "Indexing failed." : null,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        });

        await context.SaveChangesAsync();
        return new RetrievalCandidateIdentity(organizationId, documentId, chunkId, embeddingId);
    }

    private static async Task<RetrievalCandidateIdentity> SeedDocumentAndChunkWithoutEmbeddingAsync(
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

        var chunkId = Guid.NewGuid();
        context.DocumentChunks.Add(new DocumentChunk
        {
            Id = chunkId,
            DocumentId = documentId,
            OrganizationId = organizationId,
            ChunkIndex = 0,
            Text = "Chunk without embedding.",
            CharacterLength = 24,
            TokenEstimate = 4,
            CreatedAt = timestamp
        });
        await context.SaveChangesAsync();

        return new RetrievalCandidateIdentity(organizationId, documentId, chunkId, Guid.NewGuid());
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
}

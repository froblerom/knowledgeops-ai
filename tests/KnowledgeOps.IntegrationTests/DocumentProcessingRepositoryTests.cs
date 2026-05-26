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

public sealed class DocumentProcessingRepositoryTests
{
    [SqlServerFact]
    public async Task FindPendingForProcessingAsync_ReturnsUploadedDocumentsOrderedByUploadedAt_ExcludesSoftDeleted()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue22FindPending_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, repository) = await BuildAndMigrateAsync(databaseConnectionString);
            await using var _ = services;

            var timestamp = DateTimeOffset.UtcNow;
            var org = CreateOrganization("Processing Org", timestamp);
            var user = CreateUser(org.Id, $"proc-{Guid.NewGuid():N}@example.test", timestamp);
            context.AddRange(org, user);
            await context.SaveChangesAsync();

            var olderId = Guid.NewGuid();
            var newerId = Guid.NewGuid();
            var deletedId = Guid.NewGuid();
            var processingId = Guid.NewGuid();

            await InsertDocumentAsync(context, olderId, org.Id, user.Id, "older.pdf", timestamp.AddMinutes(-5), "Uploaded", null);
            await InsertDocumentAsync(context, newerId, org.Id, user.Id, "newer.pdf", timestamp, "Uploaded", null);
            await InsertDocumentAsync(context, deletedId, org.Id, user.Id, "deleted.pdf", timestamp.AddMinutes(-10), "Uploaded", timestamp.AddMinutes(-9));
            await InsertDocumentAsync(context, processingId, org.Id, user.Id, "inprogress.pdf", timestamp.AddMinutes(-8), "Processing", null);

            var pending = await repository.FindPendingForProcessingAsync(10);

            // Only Uploaded + not deleted, ordered by UploadedAt ascending (older first)
            Assert.Equal([olderId, newerId], pending.Select(d => d.DocumentId).ToArray());
            Assert.All(pending, d => Assert.Equal(DocumentProcessingStatus.Uploaded, d.ProcessingStatus));
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    [SqlServerFact]
    public async Task FindPendingForProcessingAsync_RespectsMaxCount()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue22MaxCount_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, repository) = await BuildAndMigrateAsync(databaseConnectionString);
            await using var _ = services;

            var timestamp = DateTimeOffset.UtcNow;
            var org = CreateOrganization("MaxCount Org", timestamp);
            var user = CreateUser(org.Id, $"max-{Guid.NewGuid():N}@example.test", timestamp);
            context.AddRange(org, user);
            await context.SaveChangesAsync();

            for (var i = 0; i < 5; i++)
            {
                await InsertDocumentAsync(context, Guid.NewGuid(), org.Id, user.Id, $"doc{i}.pdf", timestamp.AddMinutes(i), "Uploaded", null);
            }

            var pending = await repository.FindPendingForProcessingAsync(maxCount: 2);

            Assert.Equal(2, pending.Count);
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    [SqlServerFact]
    public async Task ClaimForProcessingAsync_TransitionsUploadedToProcessing_ReturnsDocument()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue22Claim_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, repository) = await BuildAndMigrateAsync(databaseConnectionString);
            await using var _ = services;

            var timestamp = DateTimeOffset.UtcNow;
            var org = CreateOrganization("Claim Org", timestamp);
            var user = CreateUser(org.Id, $"claim-{Guid.NewGuid():N}@example.test", timestamp);
            context.AddRange(org, user);
            await context.SaveChangesAsync();

            var docId = Guid.NewGuid();
            await InsertDocumentAsync(context, docId, org.Id, user.Id, "claim.pdf", timestamp, "Uploaded", null);

            var claimTime = timestamp.AddSeconds(1);
            var claimed = await repository.ClaimForProcessingAsync(docId, claimTime);

            Assert.NotNull(claimed);
            Assert.Equal(DocumentProcessingStatus.Processing, claimed.ProcessingStatus);
            Assert.Equal(claimTime, claimed.ProcessingStartedAt);
            Assert.Equal(claimTime, claimed.UpdatedAt);

            // Verify persisted
            var stored = await context.Documents.AsNoTracking().SingleAsync(d => d.Id == docId);
            Assert.Equal(DocumentProcessingStatus.Processing, stored.ProcessingStatus);
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    [SqlServerFact]
    public async Task ClaimForProcessingAsync_WhenAlreadyClaimed_ReturnsNull()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue22ClaimRace_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, repository) = await BuildAndMigrateAsync(databaseConnectionString);
            await using var _ = services;

            var timestamp = DateTimeOffset.UtcNow;
            var org = CreateOrganization("Race Org", timestamp);
            var user = CreateUser(org.Id, $"race-{Guid.NewGuid():N}@example.test", timestamp);
            context.AddRange(org, user);
            await context.SaveChangesAsync();

            var docId = Guid.NewGuid();
            await InsertDocumentAsync(context, docId, org.Id, user.Id, "race.pdf", timestamp, "Processing", null);

            // Document is already Processing — claim must return null (atomic guard)
            var claimed = await repository.ClaimForProcessingAsync(docId, timestamp.AddSeconds(1));

            Assert.Null(claimed);
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    [SqlServerFact]
    public async Task MarkProcessedAsync_TransitionsProcessingToProcessed_DoesNotChangeIsRetrievalEnabled()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue22MarkProcessed_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, repository) = await BuildAndMigrateAsync(databaseConnectionString);
            await using var _ = services;

            var timestamp = DateTimeOffset.UtcNow;
            var org = CreateOrganization("Processed Org", timestamp);
            var user = CreateUser(org.Id, $"processed-{Guid.NewGuid():N}@example.test", timestamp);
            context.AddRange(org, user);
            await context.SaveChangesAsync();

            var docId = Guid.NewGuid();
            await InsertDocumentAsync(context, docId, org.Id, user.Id, "process.pdf", timestamp, "Processing", null);

            var processedAt = timestamp.AddMinutes(1);
            var result = await repository.MarkProcessedAsync(docId, processedAt);

            Assert.NotNull(result);
            Assert.Equal(DocumentProcessingStatus.Processed, result.ProcessingStatus);
            Assert.Equal(processedAt, result.ProcessedAt);
            Assert.Equal(processedAt, result.UpdatedAt);
            Assert.False(result.IsRetrievalEnabled);
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    [SqlServerFact]
    public async Task MarkProcessedAsync_WhenNotProcessing_ReturnsNull()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue22MarkProcessedGuard_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, repository) = await BuildAndMigrateAsync(databaseConnectionString);
            await using var _ = services;

            var timestamp = DateTimeOffset.UtcNow;
            var org = CreateOrganization("Guard Org", timestamp);
            var user = CreateUser(org.Id, $"guard-{Guid.NewGuid():N}@example.test", timestamp);
            context.AddRange(org, user);
            await context.SaveChangesAsync();

            var docId = Guid.NewGuid();
            await InsertDocumentAsync(context, docId, org.Id, user.Id, "guard.pdf", timestamp, "Uploaded", null);

            var result = await repository.MarkProcessedAsync(docId, timestamp.AddMinutes(1));

            Assert.Null(result);
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    [SqlServerFact]
    public async Task MarkFailedAsync_TransitionsProcessingToFailed_StoresReasonDoesNotChangeIsRetrievalEnabled()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue22MarkFailed_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, repository) = await BuildAndMigrateAsync(databaseConnectionString);
            await using var _ = services;

            var timestamp = DateTimeOffset.UtcNow;
            var org = CreateOrganization("Failed Org", timestamp);
            var user = CreateUser(org.Id, $"failed-{Guid.NewGuid():N}@example.test", timestamp);
            context.AddRange(org, user);
            await context.SaveChangesAsync();

            var docId = Guid.NewGuid();
            await InsertDocumentAsync(context, docId, org.Id, user.Id, "failed.pdf", timestamp, "Processing", null);

            var failedAt = timestamp.AddMinutes(1);
            var result = await repository.MarkFailedAsync(docId, "Unsupported encoding.", failedAt);

            Assert.NotNull(result);
            Assert.Equal(DocumentProcessingStatus.Failed, result.ProcessingStatus);
            Assert.Equal("Unsupported encoding.", result.FailureReason);
            Assert.Equal(failedAt, result.UpdatedAt);
            Assert.False(result.IsRetrievalEnabled);
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    [SqlServerFact]
    public async Task MarkFailedAsync_WhenNotProcessing_ReturnsNull()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue22MarkFailedGuard_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var (services, context, repository) = await BuildAndMigrateAsync(databaseConnectionString);
            await using var _ = services;

            var timestamp = DateTimeOffset.UtcNow;
            var org = CreateOrganization("FailedGuard Org", timestamp);
            var user = CreateUser(org.Id, $"failedguard-{Guid.NewGuid():N}@example.test", timestamp);
            context.AddRange(org, user);
            await context.SaveChangesAsync();

            var docId = Guid.NewGuid();
            await InsertDocumentAsync(context, docId, org.Id, user.Id, "failguard.pdf", timestamp, "Uploaded", null);

            var result = await repository.MarkFailedAsync(docId, "Safe reason.", timestamp.AddMinutes(1));

            Assert.Null(result);
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static async Task<(ServiceProvider services, KnowledgeOpsDbContext context, IDocumentRepository repository)>
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
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
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
                {"application/pdf"}, {42L}, {"local://test/" + fileName}, {processingStatus},
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

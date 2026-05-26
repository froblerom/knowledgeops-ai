using KnowledgeOps.Application.Documents;
using KnowledgeOps.Domain.Audit;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using KnowledgeOps.Infrastructure;
using KnowledgeOps.Infrastructure.Persistence;
using KnowledgeOps.Infrastructure.Observability;
using ApplicationAuditEvent = KnowledgeOps.Application.Observability.AuditEvent;
using ApplicationAuditSeverity = KnowledgeOps.Application.Observability.AuditSeverity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeOps.IntegrationTests;

public sealed class SqlServerPersistenceTests
{
    [SqlServerFact]
    public async Task Migration_Creates_Foundation_And_Document_Metadata_Tables_And_Enforces_Core_Integrity()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue6Tests_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var options = new DbContextOptionsBuilder<KnowledgeOpsDbContext>()
                .UseSqlServer(databaseConnectionString)
                .Options;

            Guid testUserId;
            UserRoleName testUserRoleName;
            string testUserEmail;
            Guid testOrganizationId;

            await using (var context = new KnowledgeOpsDbContext(options))
            {
                await context.Database.MigrateAsync();

                var timestamp = DateTimeOffset.UtcNow;
                var organization = new Organization
                {
                    Id = Guid.NewGuid(),
                    Name = "Issue 6 Integration Organization",
                    Status = OrganizationStatus.Active,
                    CreatedAt = timestamp,
                    UpdatedAt = timestamp
                };
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organization.Id,
                    DisplayName = "Foundation User",
                    Email = $"foundation-{Guid.NewGuid():N}@example.test",
                    Status = UserStatus.Active,
                    CreatedAt = timestamp,
                    UpdatedAt = timestamp
                };

                testUserId = user.Id;
                testUserRoleName = UserRoleName.KnowledgeAdmin;
                testUserEmail = user.Email;
                testOrganizationId = organization.Id;

                context.AddRange(
                    organization,
                    user,
                    new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        RoleName = testUserRoleName,
                        AssignedAt = timestamp
                    },
                    new AuditLogEntry
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = organization.Id,
                        UserId = user.Id,
                        EventType = "FoundationValidated",
                        EntityType = "User",
                        EntityId = user.Id,
                        Message = "Issue #6 relational foundation integration validation.",
                        Severity = AuditSeverity.Info,
                        CorrelationId = Guid.NewGuid().ToString("N"),
                        CreatedAt = timestamp
                    });

                await context.SaveChangesAsync();

                Assert.Equal(
                    organization.Name,
                    await context.Organizations
                        .Where(storedOrganization => storedOrganization.Id == organization.Id)
                        .Select(storedOrganization => storedOrganization.Name)
                        .SingleAsync());
                Assert.Equal(
                    user.Email,
                    await context.Users
                        .Where(storedUser => storedUser.Id == user.Id)
                        .Select(storedUser => storedUser.Email)
                        .SingleAsync());
                Assert.Equal(
                    testUserRoleName,
                    await context.UserRoles
                        .Where(role => role.UserId == user.Id)
                        .Select(role => role.RoleName)
                        .SingleAsync());
                Assert.Equal(
                    "FoundationValidated",
                    await context.AuditLogEntries
                        .Where(auditEntry => auditEntry.EntityId == user.Id)
                        .Select(auditEntry => auditEntry.EventType)
                        .SingleAsync());
            }

            Assert.Equal(
                [
                    "__EFMigrationsHistory",
                    "audit_log_entries",
                    "documents",
                    "organizations",
                    "user_roles",
                    "users"
                ],
                await ReadTableNamesAsync(databaseConnectionString));

            await AssertDuplicateRoleRejectedAsync(options, testUserId, testUserRoleName);
            await AssertDuplicateEmailRejectedAsync(options, testOrganizationId, testUserEmail);
            await AssertUnknownOrganizationRejectedAsync(options);
            await AssertInvalidRoleRejectedAsync(options, testUserId);
        }
        finally
        {
            await DropDatabaseIfPresentAsync(baseConnectionString, databaseName);
        }
    }

    [SqlServerFact]
    public async Task Documents_PersistCanonicalMetadata_DefaultDisablement_Scope_And_Constraints()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue20DocumentTests_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = databaseConnectionString,
                    ["Jwt:Issuer"] = "KnowledgeOps",
                    ["Jwt:Audience"] = "KnowledgeOps",
                    ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-characters-long",
                    ["Jwt:ExpirationMinutes"] = "60"
                })
                .Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);

            await using var root = services.BuildServiceProvider();
            using var scope = root.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<KnowledgeOpsDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            await context.Database.MigrateAsync();

            var timestamp = DateTimeOffset.UtcNow;
            var orgA = CreateOrganization("Document Org A", timestamp);
            var orgB = CreateOrganization("Document Org B", timestamp);
            var userA = CreateUser(orgA.Id, "document-a@example.test", timestamp);
            var userB = CreateUser(orgB.Id, "document-b@example.test", timestamp);
            context.AddRange(orgA, orgB, userA, userB);
            await context.SaveChangesAsync();

            var olderId = Guid.NewGuid();
            var newerId = Guid.NewGuid();
            var deletedId = Guid.NewGuid();
            var otherOrgId = Guid.NewGuid();
            await InsertDocumentUsingDatabaseDefaultAsync(
                context, olderId, orgA.Id, userA.Id, "older.pdf", timestamp.AddMinutes(-2), null);
            await InsertDocumentUsingDatabaseDefaultAsync(
                context, newerId, orgA.Id, userA.Id, "newer.pdf", timestamp, null);
            await InsertDocumentUsingDatabaseDefaultAsync(
                context, deletedId, orgA.Id, userA.Id, "deleted.pdf", timestamp.AddMinutes(1), timestamp);
            await InsertDocumentUsingDatabaseDefaultAsync(
                context, otherOrgId, orgB.Id, userB.Id, "outside.pdf", timestamp.AddMinutes(2), null);

            Assert.False(await context.Documents
                .Where(document => document.Id == newerId)
                .Select(document => document.IsRetrievalEnabled)
                .SingleAsync());

            var scopedDocuments = await repository.ListAsync(orgA.Id);
            Assert.Equal([newerId, olderId], scopedDocuments.Select(document => document.DocumentId).ToArray());
            Assert.Null(await repository.FindAsync(otherOrgId, orgA.Id));
            Assert.Null(await repository.FindAsync(deletedId, orgA.Id));

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [documents] SET [is_retrieval_enabled] = {true} WHERE [document_id] = {newerId}");
            var changed = await repository.DisableRetrievalAsync(newerId, orgA.Id, timestamp.AddMinutes(3));
            var repeated = await repository.DisableRetrievalAsync(newerId, orgA.Id, timestamp.AddMinutes(4));
            Assert.NotNull(changed);
            Assert.True(changed.WasChanged);
            Assert.False(changed.Document.IsRetrievalEnabled);
            Assert.NotNull(repeated);
            Assert.False(repeated.WasChanged);
            Assert.Equal(DocumentProcessingStatus.Uploaded, repeated.Document.ProcessingStatus);

            await AssertInvalidDocumentStatusRejectedAsync(context, orgA.Id, userA.Id, timestamp);
            await AssertNullStorageLocationRejectedAsync(context, orgA.Id, userA.Id, timestamp);
        }
        finally
        {
            await DropDatabaseIfPresentAsync(baseConnectionString, databaseName);
        }
    }

    [SqlServerFact]
    public async Task AuditWriter_PersistsSafeOperationalEventUsingExistingTable()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue15AuditTests_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var options = new DbContextOptionsBuilder<KnowledgeOpsDbContext>()
                .UseSqlServer(databaseConnectionString)
                .Options;

            await using var context = new KnowledgeOpsDbContext(options);
            await context.Database.MigrateAsync();

            var writer = new EfAuditEventWriter(context);
            await writer.WriteAsync(new ApplicationAuditEvent(
                "UserLoginFailure",
                "User login failed.",
                ApplicationAuditSeverity.Warning,
                "unsafe correlation id"));

            var stored = await context.AuditLogEntries
                .Where(entry => entry.EventType == "UserLoginFailure")
                .SingleAsync();

            Assert.Equal("User login failed.", stored.Message);
            Assert.Equal(AuditSeverity.Warning, stored.Severity);
            Assert.NotEqual("unsafe correlation id", stored.CorrelationId);
            Assert.Equal(32, stored.CorrelationId!.Length);
        }
        finally
        {
            await DropDatabaseIfPresentAsync(baseConnectionString, databaseName);
        }
    }

    private static async Task AssertDuplicateRoleRejectedAsync(
        DbContextOptions<KnowledgeOpsDbContext> options,
        Guid userId,
        UserRoleName existingRoleName)
    {
        await using var context = new KnowledgeOpsDbContext(options);

        context.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleName = existingRoleName,
            AssignedAt = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task AssertUnknownOrganizationRejectedAsync(
        DbContextOptions<KnowledgeOpsDbContext> options)
    {
        await using var context = new KnowledgeOpsDbContext(options);
        var timestamp = DateTimeOffset.UtcNow;

        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            DisplayName = "Invalid Scope User",
            Email = $"invalid-scope-{Guid.NewGuid():N}@example.test",
            Status = UserStatus.Active,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task AssertDuplicateEmailRejectedAsync(
        DbContextOptions<KnowledgeOpsDbContext> options,
        Guid organizationId,
        string existingEmail)
    {
        await using var context = new KnowledgeOpsDbContext(options);
        var timestamp = DateTimeOffset.UtcNow;

        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            DisplayName = "Duplicate Email User",
            Email = existingEmail,
            Status = UserStatus.Active,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task AssertInvalidRoleRejectedAsync(
        DbContextOptions<KnowledgeOpsDbContext> options,
        Guid userId)
    {
        await using var context = new KnowledgeOpsDbContext(options);

        context.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleName = (UserRoleName)999,
            AssignedAt = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

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

    private static Task<int> InsertDocumentUsingDatabaseDefaultAsync(
        KnowledgeOpsDbContext context,
        Guid documentId,
        Guid organizationId,
        Guid uploadedByUserId,
        string fileName,
        DateTimeOffset uploadedAt,
        DateTimeOffset? deletedAt) =>
        context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [documents] (
                [document_id], [organization_id], [uploaded_by_user_id], [file_name], [title],
                [content_type], [file_size_bytes], [storage_location], [processing_status],
                [uploaded_at], [created_at], [updated_at], [deleted_at])
            VALUES (
                {documentId}, {organizationId}, {uploadedByUserId}, {fileName}, {fileName},
                {"application/pdf"}, {42L}, {"pending://document-metadata-only"}, {"Uploaded"},
                {uploadedAt.UtcDateTime}, {uploadedAt.UtcDateTime}, {uploadedAt.UtcDateTime},
                {(deletedAt.HasValue ? deletedAt.Value.UtcDateTime : null)});
            """);

    private static async Task AssertInvalidDocumentStatusRejectedAsync(
        KnowledgeOpsDbContext context,
        Guid organizationId,
        Guid userId,
        DateTimeOffset timestamp)
    {
        await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [documents] (
                [document_id], [organization_id], [uploaded_by_user_id], [file_name], [title],
                [content_type], [file_size_bytes], [storage_location], [processing_status],
                [uploaded_at], [created_at], [updated_at])
            VALUES (
                {Guid.NewGuid()}, {organizationId}, {userId}, {"invalid.pdf"}, {"Invalid"},
                {"application/pdf"}, {42L}, {"pending://document-metadata-only"}, {"Disabled"},
                {timestamp.UtcDateTime}, {timestamp.UtcDateTime}, {timestamp.UtcDateTime});
            """));
    }

    private static async Task AssertNullStorageLocationRejectedAsync(
        KnowledgeOpsDbContext context,
        Guid organizationId,
        Guid userId,
        DateTimeOffset timestamp)
    {
        await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [documents] (
                [document_id], [organization_id], [uploaded_by_user_id], [file_name], [title],
                [content_type], [file_size_bytes], [storage_location], [processing_status],
                [uploaded_at], [created_at], [updated_at])
            VALUES (
                {Guid.NewGuid()}, {organizationId}, {userId}, {"null-storage.pdf"}, {"Null Storage"},
                {"application/pdf"}, {42L}, {(string?)null}, {"Uploaded"},
                {timestamp.UtcDateTime}, {timestamp.UtcDateTime}, {timestamp.UtcDateTime});
            """));
    }

    private static async Task<string[]> ReadTableNamesAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT [name]
            FROM sys.tables
            ORDER BY [name];
            """;

        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names.ToArray();
    }

    private static string WithDatabase(string connectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = databaseName
        };

        return builder.ConnectionString;
    }

    private static async Task DropDatabaseIfPresentAsync(
        string connectionString,
        string databaseName)
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

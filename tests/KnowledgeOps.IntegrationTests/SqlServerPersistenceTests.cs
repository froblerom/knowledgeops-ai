using KnowledgeOps.Domain.Audit;
using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.IntegrationTests;

public sealed class SqlServerPersistenceTests
{
    [SqlServerFact]
    public async Task Migration_Creates_Only_Foundation_Tables_And_Enforces_Core_Integrity()
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

                context.AddRange(
                    organization,
                    user,
                    new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        RoleName = UserRoleName.KnowledgeAdmin,
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
                    UserRoleName.KnowledgeAdmin,
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
                    "organizations",
                    "user_roles",
                    "users"
                ],
                await ReadTableNamesAsync(databaseConnectionString));

            await AssertDuplicateRoleRejectedAsync(options);
            await AssertDuplicateEmailRejectedAsync(options);
            await AssertUnknownOrganizationRejectedAsync(options);
            await AssertInvalidRoleRejectedAsync(options);
        }
        finally
        {
            await DropDatabaseIfPresentAsync(baseConnectionString, databaseName);
        }
    }

    private static async Task AssertDuplicateRoleRejectedAsync(
        DbContextOptions<KnowledgeOpsDbContext> options)
    {
        await using var context = new KnowledgeOpsDbContext(options);
        var existingRole = await context.UserRoles.SingleAsync();

        context.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = existingRole.UserId,
            RoleName = existingRole.RoleName,
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
        DbContextOptions<KnowledgeOpsDbContext> options)
    {
        await using var context = new KnowledgeOpsDbContext(options);
        var storedUser = await context.Users.SingleAsync();
        var timestamp = DateTimeOffset.UtcNow;

        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = storedUser.OrganizationId,
            DisplayName = "Duplicate Email User",
            Email = storedUser.Email,
            Status = UserStatus.Active,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task AssertInvalidRoleRejectedAsync(
        DbContextOptions<KnowledgeOpsDbContext> options)
    {
        await using var context = new KnowledgeOpsDbContext(options);
        var userId = await context.Users.Select(user => user.Id).SingleAsync();

        context.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleName = (UserRoleName)999,
            AssignedAt = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
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

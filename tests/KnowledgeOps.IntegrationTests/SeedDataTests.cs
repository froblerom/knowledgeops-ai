using KnowledgeOps.Domain.Users;
using KnowledgeOps.Infrastructure.Persistence;
using KnowledgeOps.Infrastructure.Persistence.SeedData;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.IntegrationTests;

public sealed class SeedDataTests
{
    [SqlServerFact]
    public async Task Seed_Contains_Exactly_Two_Organizations()
    {
        await RunInSeedDatabaseAsync(async context =>
        {
            var count = await context.Organizations.CountAsync();
            Assert.Equal(2, count);
        });
    }

    [SqlServerFact]
    public async Task Seed_Contains_Both_Named_Organizations()
    {
        await RunInSeedDatabaseAsync(async context =>
        {
            Assert.True(await context.Organizations.AnyAsync(
                org => org.Id == SeedDataIds.AsteriaOrganizationId
                    && org.Name == "Asteria Support Group"));
            Assert.True(await context.Organizations.AnyAsync(
                org => org.Id == SeedDataIds.BorealOrganizationId
                    && org.Name == "Boreal Contact Services"));
        });
    }

    [SqlServerFact]
    public async Task Seed_Contains_Exactly_Seven_Users()
    {
        await RunInSeedDatabaseAsync(async context =>
        {
            var count = await context.Users.CountAsync();
            Assert.Equal(7, count);
        });
    }

    [SqlServerFact]
    public async Task Seed_Asteria_Has_Five_Users()
    {
        await RunInSeedDatabaseAsync(async context =>
        {
            var count = await context.Users
                .Where(user => user.OrganizationId == SeedDataIds.AsteriaOrganizationId)
                .CountAsync();
            Assert.Equal(5, count);
        });
    }

    [SqlServerFact]
    public async Task Seed_Boreal_Has_Two_Users()
    {
        await RunInSeedDatabaseAsync(async context =>
        {
            var count = await context.Users
                .Where(user => user.OrganizationId == SeedDataIds.BorealOrganizationId)
                .CountAsync();
            Assert.Equal(2, count);
        });
    }

    [SqlServerFact]
    public async Task Seed_Users_Have_Expected_Emails()
    {
        await RunInSeedDatabaseAsync(async context =>
        {
            var emails = await context.Users
                .OrderBy(user => user.Email)
                .Select(user => user.Email)
                .ToArrayAsync();

            Assert.Equal(
                [
                    "admin.a@asteria.example.com",
                    "admin.b@boreal.example.com",
                    "agent.a@asteria.example.com",
                    "agent.b@boreal.example.com",
                    "knowledgeadmin.a@asteria.example.com",
                    "manager.a@asteria.example.com",
                    "supervisor.a@asteria.example.com"
                ],
                emails);
        });
    }

    [SqlServerFact]
    public async Task Seed_Contains_Exactly_Seven_Role_Assignments()
    {
        await RunInSeedDatabaseAsync(async context =>
        {
            var count = await context.UserRoles.CountAsync();
            Assert.Equal(7, count);
        });
    }

    [SqlServerFact]
    public async Task Seed_All_Five_MVP_Roles_Are_Represented()
    {
        await RunInSeedDatabaseAsync(async context =>
        {
            var distinctRoles = await context.UserRoles
                .Select(role => role.RoleName)
                .Distinct()
                .OrderBy(roleName => roleName)
                .ToArrayAsync();

            Assert.Equal(
                [
                    UserRoleName.Admin,
                    UserRoleName.Agent,
                    UserRoleName.KnowledgeAdmin,
                    UserRoleName.Manager,
                    UserRoleName.Supervisor
                ],
                distinctRoles);
        });
    }

    [SqlServerFact]
    public async Task Seed_No_Passwords_Are_Stored()
    {
        await RunInSeedDatabaseAsync(async context =>
        {
            var anyWithPassword = await context.Users
                .AnyAsync(user => user.PasswordHash != null);
            Assert.False(anyWithPassword);
        });
    }

    [SqlServerFact]
    public async Task Seed_Agent_A_And_Agent_B_Are_In_Different_Organizations()
    {
        await RunInSeedDatabaseAsync(async context =>
        {
            var agentA = await context.Users.SingleAsync(
                user => user.Id == SeedDataIds.AsteriaAgentUserId);
            var agentB = await context.Users.SingleAsync(
                user => user.Id == SeedDataIds.BorealAgentUserId);

            Assert.NotEqual(agentA.OrganizationId, agentB.OrganizationId);
            Assert.Equal(SeedDataIds.AsteriaOrganizationId, agentA.OrganizationId);
            Assert.Equal(SeedDataIds.BorealOrganizationId, agentB.OrganizationId);
        });
    }

    [SqlServerFact]
    public async Task Seed_Cross_Organization_Personas_Cover_Required_Roles()
    {
        await RunInSeedDatabaseAsync(async context =>
        {
            // Asteria Agent
            Assert.True(await context.UserRoles.AnyAsync(
                role => role.Id == SeedDataIds.AsteriaAgentRoleId
                     && role.UserId == SeedDataIds.AsteriaAgentUserId
                     && role.RoleName == UserRoleName.Agent));

            // Asteria Admin
            Assert.True(await context.UserRoles.AnyAsync(
                role => role.Id == SeedDataIds.AsteriaAdminRoleId
                     && role.UserId == SeedDataIds.AsteriaAdminUserId
                     && role.RoleName == UserRoleName.Admin));

            // Boreal Agent
            Assert.True(await context.UserRoles.AnyAsync(
                role => role.Id == SeedDataIds.BorealAgentRoleId
                     && role.UserId == SeedDataIds.BorealAgentUserId
                     && role.RoleName == UserRoleName.Agent));

            // Boreal Admin
            Assert.True(await context.UserRoles.AnyAsync(
                role => role.Id == SeedDataIds.BorealAdminRoleId
                     && role.UserId == SeedDataIds.BorealAdminUserId
                     && role.RoleName == UserRoleName.Admin));
        });
    }

    [SqlServerFact]
    public async Task Seed_No_Audit_Log_Entries_Exist()
    {
        await RunInSeedDatabaseAsync(async context =>
        {
            var count = await context.AuditLogEntries.CountAsync();
            Assert.Equal(0, count);
        });
    }

    [SqlServerFact]
    public async Task Seed_No_Unexpected_Tables_Exist()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsSeedSchemaTests_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var options = new DbContextOptionsBuilder<KnowledgeOpsDbContext>()
                .UseSqlServer(databaseConnectionString)
                .Options;

            await using var context = new KnowledgeOpsDbContext(options);
            await context.Database.MigrateAsync();

            var tableNames = await ReadTableNamesAsync(databaseConnectionString);

            Assert.Equal(
                [
                    "__EFMigrationsHistory",
                    "audit_log_entries",
                    "documents",
                    "organizations",
                    "user_roles",
                    "users"
                ],
                tableNames);
        }
        finally
        {
            await DropDatabaseIfPresentAsync(baseConnectionString, databaseName);
        }
    }

    private static async Task RunInSeedDatabaseAsync(
        Func<KnowledgeOpsDbContext, Task> testBody)
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsSeedDataTests_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var options = new DbContextOptionsBuilder<KnowledgeOpsDbContext>()
                .UseSqlServer(databaseConnectionString)
                .Options;

            await using var context = new KnowledgeOpsDbContext(options);
            await context.Database.MigrateAsync();
            await testBody(context);
        }
        finally
        {
            await DropDatabaseIfPresentAsync(baseConnectionString, databaseName);
        }
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

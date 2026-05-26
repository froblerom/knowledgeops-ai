using KnowledgeOps.Application;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Application.Users;
using KnowledgeOps.Domain.Users;
using KnowledgeOps.Infrastructure;
using KnowledgeOps.Infrastructure.Persistence;
using KnowledgeOps.Infrastructure.Persistence.SeedData;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeOps.IntegrationTests.Users;

public sealed class UserManagementIntegrationTests
{
    private static string BaseConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;

    [SqlServerFact]
    public async Task Create_PersistsNormalizedEmailHashAndSafeAudit()
    {
        await RunAsync(async provider =>
        {
            var service = provider.GetRequiredService<UserManagementService>();
            var context = provider.GetRequiredService<KnowledgeOpsDbContext>();

            var created = await service.CreateAsync(
                new UserManagementActor(SeedDataIds.AsteriaAdminUserId, SeedDataIds.AsteriaOrganizationId),
                new CreateManagedUserCommand(
                    "Created Agent",
                    " Created.Agent@Example.Test ",
                    SeedDataIds.AsteriaOrganizationId,
                    "Active",
                    ["Agent"],
                    "bootstrap-only"));

            var stored = await context.Users.AsNoTracking().SingleAsync(user => user.Id == created.UserId);
            Assert.Equal("created.agent@example.test", stored.Email);
            Assert.NotNull(stored.PasswordHash);
            Assert.NotEqual("bootstrap-only", stored.PasswordHash);
            Assert.DoesNotContain("bootstrap-only", created.ToString(), StringComparison.Ordinal);

            var audit = await context.AuditLogEntries
                .Where(entry => entry.EntityId == created.UserId)
                .Select(entry => new { entry.EventType, entry.Message })
                .ToArrayAsync();
            Assert.Contains(audit, entry => entry.EventType == AuditEventTypes.UserCreated);
            Assert.DoesNotContain(audit, entry => entry.Message.Contains("bootstrap-only", StringComparison.Ordinal));
        });
    }

    [SqlServerFact]
    public async Task Mutations_CannotDisableOrRemoveFinalActiveAdmin()
    {
        await RunAsync(async provider =>
        {
            var repository = provider.GetRequiredService<IUserManagementRepository>();
            var context = provider.GetRequiredService<KnowledgeOpsDbContext>();

            var statusResult = await repository.UpdateAsync(
                SeedDataIds.AsteriaAdminUserId,
                SeedDataIds.AsteriaOrganizationId,
                new ManagedUserUpdate(
                    "Asteria Admin",
                    "admin.a@asteria.example.com",
                    UserStatus.Disabled,
                    DateTimeOffset.UtcNow));
            var roleResult = await repository.RemoveRoleAsync(
                SeedDataIds.AsteriaAdminUserId,
                SeedDataIds.AsteriaOrganizationId,
                UserRoleName.Admin);

            Assert.Equal(UserWriteOutcome.FinalActiveAdmin, statusResult.Outcome);
            Assert.Equal(UserWriteOutcome.FinalActiveAdmin, roleResult.Outcome);
            Assert.Equal(
                UserStatus.Active,
                await context.Users
                    .Where(user => user.Id == SeedDataIds.AsteriaAdminUserId)
                    .Select(user => user.Status)
                    .SingleAsync());
            Assert.True(await context.UserRoles.AnyAsync(
                role => role.UserId == SeedDataIds.AsteriaAdminUserId
                    && role.RoleName == UserRoleName.Admin));
        });
    }

    private static async Task RunAsync(Func<IServiceProvider, Task> test)
    {
        var databaseName = $"KnowledgeOpsUserManagementTests_{Guid.NewGuid():N}";
        var connectionString = WithDatabase(BaseConnectionString, databaseName);

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                    ["Jwt:Issuer"] = "KnowledgeOps",
                    ["Jwt:Audience"] = "KnowledgeOps",
                    ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-characters-long",
                    ["Jwt:ExpirationMinutes"] = "60"
                })
                .Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplication();
            services.AddInfrastructure(configuration);
            services.AddSingleton<ICorrelationContext, StubCorrelationContext>();

            await using var root = services.BuildServiceProvider();
            using var scope = root.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<KnowledgeOpsDbContext>();
            await context.Database.MigrateAsync();
            await test(scope.ServiceProvider);
        }
        finally
        {
            await DropDatabaseIfPresentAsync(BaseConnectionString, databaseName);
        }
    }

    private static string WithDatabase(string connectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = databaseName };
        return builder.ConnectionString;
    }

    private static async Task DropDatabaseIfPresentAsync(string connectionString, string databaseName)
    {
        await using var connection = new SqlConnection(WithDatabase(connectionString, "master"));
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

    private sealed class StubCorrelationContext : ICorrelationContext
    {
        public string CorrelationId => "integration-safe-id";
    }
}

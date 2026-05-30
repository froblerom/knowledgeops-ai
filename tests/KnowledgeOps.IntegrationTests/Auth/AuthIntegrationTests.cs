using KnowledgeOps.Application;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Auth.Commands;
using KnowledgeOps.Domain.Users;
using KnowledgeOps.Infrastructure;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.IntegrationTests.Auth;

public sealed class AuthIntegrationTests
{
    private static string BaseConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;

    [SqlServerFact]
    public async Task Login_ActiveUserWithPassword_Succeeds()
    {
        await RunInDatabaseAsync(async (context, handler, hasher) =>
        {
            await ProvisionPasswordAsync(context, hasher, "agent.a@asteria.example.com", "test-password-1");

            var result = await handler.HandleAsync(
                new LoginCommand("agent.a@asteria.example.com", "test-password-1"));

            Assert.NotNull(result);
            Assert.Equal("agent.a@asteria.example.com", result.Email);
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
        });
    }

    [SqlServerFact]
    public async Task Login_ActiveUser_UpdatesLastLoginAt()
    {
        await RunInDatabaseAsync(async (context, handler, hasher) =>
        {
            await ProvisionPasswordAsync(context, hasher, "agent.a@asteria.example.com", "test-password-2");

            var before = DateTimeOffset.UtcNow;
            await handler.HandleAsync(new LoginCommand("agent.a@asteria.example.com", "test-password-2"));
            var after = DateTimeOffset.UtcNow;

            var user = await context.Users
                .AsNoTracking()
                .SingleAsync(u => u.Email == "agent.a@asteria.example.com");

            Assert.NotNull(user.LastLoginAt);
            Assert.True(user.LastLoginAt >= before && user.LastLoginAt <= after);
        });
    }

    [SqlServerFact]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        await RunInDatabaseAsync(async (context, handler, hasher) =>
        {
            await ProvisionPasswordAsync(context, hasher, "agent.a@asteria.example.com", "test-password-3");

            var result = await handler.HandleAsync(
                new LoginCommand("agent.a@asteria.example.com", "wrong-password"));

            Assert.Null(result);
        });
    }

    [SqlServerFact]
    public async Task Login_UserWithNullHash_ReturnsNull()
    {
        await RunInDatabaseAsync(async (context, handler, _) =>
        {
            // Seed users start with null password_hash — login must fail without provisioning
            var result = await handler.HandleAsync(
                new LoginCommand("agent.a@asteria.example.com", "any-password"));

            Assert.Null(result);
        });
    }

    [SqlServerFact]
    public async Task Login_DisabledUser_ReturnsNull()
    {
        await RunInDatabaseAsync(async (context, handler, hasher) =>
        {
            await ProvisionPasswordAsync(context, hasher, "agent.a@asteria.example.com", "test-password-5");
            await SetUserStatusAsync(context, "agent.a@asteria.example.com", UserStatus.Disabled);

            var result = await handler.HandleAsync(
                new LoginCommand("agent.a@asteria.example.com", "test-password-5"));

            Assert.Null(result);
        });
    }

    private static async Task RunInDatabaseAsync(
        Func<KnowledgeOpsDbContext, LoginCommandHandler, IPasswordHasher, Task> testBody)
    {
        var databaseName = $"KnowledgeOpsAuthTests_{Guid.NewGuid():N}";
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
            services.AddSingleton<IConfiguration>(configuration);
            services.AddApplication();
            services.AddInfrastructure(configuration);
            services.AddJwtInfrastructure();

            await using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<KnowledgeOpsDbContext>();
            await context.Database.MigrateAsync();

            var repo = scope.ServiceProvider.GetRequiredService<IUserAuthRepository>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var handler = new LoginCommandHandler(
                repo, passwordHasher, tokenService,
                NullLogger<LoginCommandHandler>.Instance);

            await testBody(context, handler, passwordHasher);
        }
        finally
        {
            await DropDatabaseIfPresentAsync(BaseConnectionString, databaseName);
        }
    }

    private static async Task ProvisionPasswordAsync(
        KnowledgeOpsDbContext context,
        IPasswordHasher hasher,
        string email,
        string password)
    {
        var hash = hasher.HashPassword(password);
        await context.Users
            .Where(u => u.Email == email)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, hash));
    }

    private static async Task SetUserStatusAsync(
        KnowledgeOpsDbContext context, string email, UserStatus status)
    {
        await context.Users
            .Where(u => u.Email == email)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, status));
    }

    private static string WithDatabase(string connectionString, string databaseName)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = databaseName
        };
        return builder.ConnectionString;
    }

    private static async Task DropDatabaseIfPresentAsync(string connectionString, string databaseName)
    {
        var masterConnectionString = WithDatabase(connectionString, "master");

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(masterConnectionString);
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

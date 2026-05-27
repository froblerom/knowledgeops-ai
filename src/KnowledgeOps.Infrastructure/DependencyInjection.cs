using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Embeddings;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Users;
using KnowledgeOps.Infrastructure.Auth;
using KnowledgeOps.Infrastructure.Authorization;
using KnowledgeOps.Infrastructure.Documents;
using KnowledgeOps.Infrastructure.Embeddings;
using KnowledgeOps.Infrastructure.Observability;
using KnowledgeOps.Infrastructure.Persistence;
using KnowledgeOps.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<KnowledgeOpsDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "SQL Server persistence requires configuration key 'ConnectionStrings:DefaultConnection'. " +
                    "For local development, set environment variable 'ConnectionStrings__DefaultConnection'.");
            }

            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IUserAuthRepository, UserAuthRepository>();
        services.AddScoped<IUserAccessStateReader, EfUserAccessStateReader>();
        services.AddScoped<IDocumentRepository, EfDocumentRepository>();
        services.AddScoped<IDocumentChunkRepository, EfDocumentChunkRepository>();
        services.AddScoped<IChunkEmbeddingRepository, EfChunkEmbeddingRepository>();
        services.AddScoped<IEmbeddingProvider, FakeEmbeddingProvider>();
        services.AddOptions<FakeEmbeddingProviderSettings>().BindConfiguration("Embeddings:Fake");
        services.AddScoped<IDocumentStorage, LocalDocumentStorage>();
        services.AddScoped<IDocumentTextExtractor, TxtMarkdownTextExtractor>();
        services.AddScoped<IDocumentChunker, DocumentChunker>();
        services.AddScoped<IDocumentProcessingTransactionFactory, EfDocumentProcessingTransactionFactory>();
        services.AddOptions<LocalStorageSettings>().BindConfiguration("Storage");
        services.AddScoped<IUserManagementRepository, EfUserManagementRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
        services.AddScoped<IDatabaseHealthCheck, EfDatabaseHealthCheck>();

        return services;
    }

    /// <summary>
    /// Registers JWT settings validation. Call only from API hosts that authenticate requests.
    /// The Worker host does not handle JWT and must not call this method.
    /// </summary>
    public static IServiceCollection AddJwtInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<JwtSettings>()
            .BindConfiguration("Jwt")
            .Validate(
                s => !string.IsNullOrWhiteSpace(s.SigningKey) && s.SigningKey.Length >= 32,
                "JWT signing key is missing or too short. Set 'Jwt:SigningKey' to a random value " +
                "of at least 32 characters using dotnet user-secrets or an environment variable. " +
                "Never commit a real signing key to source control.")
            .ValidateOnStart();

        return services;
    }
}

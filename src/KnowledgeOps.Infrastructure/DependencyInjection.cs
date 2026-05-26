using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Infrastructure.Auth;
using KnowledgeOps.Infrastructure.Observability;
using KnowledgeOps.Infrastructure.Persistence;
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

        services.AddOptions<JwtSettings>()
            .BindConfiguration("Jwt")
            .Validate(
                s => !string.IsNullOrWhiteSpace(s.SigningKey) && s.SigningKey.Length >= 32,
                "JWT signing key is missing or too short. Set 'Jwt:SigningKey' to a random value " +
                "of at least 32 characters using dotnet user-secrets or an environment variable. " +
                "Never commit a real signing key to source control.")
            .ValidateOnStart();

        services.AddScoped<IUserAuthRepository, UserAuthRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
        services.AddScoped<IDatabaseHealthCheck, EfDatabaseHealthCheck>();

        return services;
    }
}

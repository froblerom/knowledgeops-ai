using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Admin;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Application.Chat.Citations;
using KnowledgeOps.Application.Chat.Feedback;
using KnowledgeOps.Application.Dashboard;
using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Embeddings;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Application.Retrieval;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Users;
using KnowledgeOps.Infrastructure.Auth;
using KnowledgeOps.Infrastructure.Admin;
using KnowledgeOps.Infrastructure.Authorization;
using KnowledgeOps.Infrastructure.Chat;
using KnowledgeOps.Infrastructure.Dashboard;
using KnowledgeOps.Infrastructure.Documents;
using KnowledgeOps.Infrastructure.Embeddings;
using KnowledgeOps.Infrastructure.Observability;
using KnowledgeOps.Infrastructure.Persistence;
using KnowledgeOps.Infrastructure.Retrieval;
using KnowledgeOps.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton(configuration);

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
        services.AddScoped<LocalVectorStore>();
        services.AddScoped<IRetrievalIndex>(provider => provider.GetRequiredService<LocalVectorStore>());
        services.AddScoped<ISemanticSearchProvider>(provider => provider.GetRequiredService<LocalVectorStore>());
        services.AddScoped<IRetrievalEligibilityRepository, EfRetrievalEligibilityRepository>();
        services.AddOptions<RetrievalSettings>().BindConfiguration("Retrieval");
        services.AddScoped<IDocumentStorage, LocalDocumentStorage>();
        services.AddScoped<IDocumentTextExtractor, TxtMarkdownTextExtractor>();
        services.AddScoped<IDocumentChunker, DocumentChunker>();
        services.AddScoped<IDocumentProcessingTransactionFactory, EfDocumentProcessingTransactionFactory>();
        services.AddOptions<LocalStorageSettings>().BindConfiguration("Storage");
        services.AddScoped<IUserManagementRepository, EfUserManagementRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IChatSessionRepository, EfChatSessionRepository>();
        services.AddScoped<IChatInteractionRepository, EfChatInteractionRepository>();
        services.AddScoped<IAnswerFeedbackRepository, EfAnswerFeedbackRepository>();
        services.AddScoped<IChunkTextReader, EfChunkTextReader>();
        services.AddScoped<ICitationRepository, EfCitationRepository>();
        services.AddScoped<IDocumentTitleReader, EfDocumentTitleReader>();
        services.AddScoped<IDashboardRepository, EfDashboardRepository>();
        services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();
        services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
        services.AddScoped<IDatabaseHealthCheck, EfDatabaseHealthCheck>();
        services.AddScoped<IRetrievalStorageHealthCheck, LocalRetrievalStorageHealthCheck>();

        return services;
    }

    /// <summary>
    /// Registers the answer generation provider based on the Ai:AnswerProvider configuration value.
    /// Call only from the API host. The Worker host does not perform answer generation.
    /// Supported values:
    ///   Demo (default, no API key required)
    ///   OpenAI (requires Ai:OpenAI:ApiKey)
    ///   LocalOpenAICompatible (requires Ai:LocalOpenAICompatible:BaseUrl; no API key required for Ollama)
    /// </summary>
    public static IServiceCollection AddAiAnswerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var providerValue = (configuration["Ai:AnswerProvider"] ?? "Demo").Trim();

        if (string.Equals(providerValue, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = configuration["Ai:OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Ai:AnswerProvider is 'OpenAI' but 'Ai:OpenAI:ApiKey' is not configured. " +
                    "Set it via dotnet user-secrets or the environment variable Ai__OpenAI__ApiKey. " +
                    "Never commit an API key to source control.");

            var openAiModel = configuration["Ai:OpenAI:Model"] ?? "gpt-4.1-mini";
            services.AddSingleton<IAiProviderDiagnostics>(
                new AiProviderDiagnosticsService("OpenAI", true, openAiModel));
            services.AddScoped<IAiAnswerGenerator, OpenAIAnswerGenerator>();
            services.AddOptions<OpenAIAnswerGeneratorSettings>().BindConfiguration("Ai:OpenAI");
        }
        else if (string.Equals(providerValue, "Demo", StringComparison.OrdinalIgnoreCase))
        {
            var demoModel = configuration["Ai:Demo:ModelName"] ?? "demo-extractive-v1";
            services.AddSingleton<IAiProviderDiagnostics>(
                new AiProviderDiagnosticsService("Demo", false, demoModel));
            services.AddScoped<IAiAnswerGenerator, DemoGroundedAnswerGenerator>();
            services.AddOptions<DemoGroundedAnswerGeneratorSettings>().BindConfiguration("Ai:Demo");
        }
        else if (string.Equals(providerValue, "LocalOpenAICompatible", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = configuration["Ai:LocalOpenAICompatible:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException(
                    "Ai:AnswerProvider is 'LocalOpenAICompatible' but " +
                    "'Ai:LocalOpenAICompatible:BaseUrl' is not configured. " +
                    "Set it in appsettings.json or via the environment variable " +
                    "Ai__LocalOpenAICompatible__BaseUrl. " +
                    "For Ollama: http://localhost:11434/v1");

            var localModel = configuration["Ai:LocalOpenAICompatible:Model"] ?? "qwen3:8b";
            services.AddSingleton<IAiProviderDiagnostics>(
                new AiProviderDiagnosticsService(
                    "LocalOpenAICompatible", false, localModel,
                    localProviderBaseUrl: baseUrl));
            services.AddScoped<IAiAnswerGenerator, LocalOpenAICompatibleAnswerGenerator>();
            services.AddOptions<LocalOpenAICompatibleAnswerGeneratorSettings>()
                .BindConfiguration("Ai:LocalOpenAICompatible");
        }
        else
        {
            throw new InvalidOperationException(
                $"Unknown Ai:AnswerProvider value '{providerValue}'. " +
                $"Supported values: Demo, OpenAI, LocalOpenAICompatible.");
        }

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

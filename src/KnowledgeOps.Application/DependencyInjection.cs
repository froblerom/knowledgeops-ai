using KnowledgeOps.Application.Auth.Commands;
using KnowledgeOps.Application.Auth.Queries;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeOps.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<GetCurrentUserQueryHandler>();
        services.AddScoped<DocumentService>();
        services.AddScoped<IDocumentProcessingOrchestrator, DocumentProcessingOrchestrator>();
        services.AddScoped<IDocumentProcessingStep, ExtractAndChunkDocumentProcessingStep>();
        services.AddScoped<IDocumentProcessingStep, GenerateChunkEmbeddingsProcessingStep>();
        services.AddScoped<UserManagementService>();

        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddSingleton<IOrganizationScopeService, OrganizationScopeService>();

        return services;
    }
}

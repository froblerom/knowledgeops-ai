using KnowledgeOps.Application.Auth.Commands;
using KnowledgeOps.Application.Auth.Queries;
using KnowledgeOps.Application.Admin;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Authorization.Hooks;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Application.Chat.Citations;
using KnowledgeOps.Application.Chat.Feedback;
using KnowledgeOps.Application.Chat.Prompting;
using KnowledgeOps.Application.Dashboard;
using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Retrieval;
using KnowledgeOps.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeOps.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Application-layer services required by all hosts (API and Worker).
    /// No service registered here may depend on ICurrentUser.
    /// </summary>
    public static IServiceCollection AddApplicationCore(this IServiceCollection services)
    {
        services.AddScoped<IDocumentProcessingOrchestrator, DocumentProcessingOrchestrator>();
        services.AddScoped<IDocumentProcessingStep, ExtractAndChunkDocumentProcessingStep>();
        services.AddScoped<IDocumentProcessingStep, GenerateChunkEmbeddingsProcessingStep>();
        services.AddScoped<IDocumentProcessingStep, IndexDocumentChunkEmbeddingsProcessingStep>();
        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddSingleton<IOrganizationScopeService, OrganizationScopeService>();
        return services;
    }

    /// <summary>
    /// Registers Application-layer services that require an authenticated HTTP request context
    /// (ICurrentUser). Call this only from the API host. Do not call from the Worker host.
    /// </summary>
    public static IServiceCollection AddApplicationApiFeatures(this IServiceCollection services)
    {
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<GetCurrentUserQueryHandler>();
        services.AddScoped<DocumentService>();
        services.AddScoped<UserManagementService>();
        services.AddScoped<IEligibleSemanticRetrievalService, EligibleSemanticRetrievalService>();
        services.AddSingleton<IPromptAuthorizationFilter, DefaultPromptAuthorizationFilter>();
        services.AddSingleton<ICitationAuthorizationFilter, DefaultCitationAuthorizationFilter>();
        services.AddScoped<IGroundedPromptBuilder, GroundedPromptBuilder>();
        services.AddScoped<IContextSufficiencyPolicy, ContextSufficiencyPolicy>();
        services.AddScoped<ICitationMapper, CitationMapper>();
        services.AddScoped<IRagChatOrchestrationService, RagChatOrchestrationService>();
        services.AddScoped<IAnswerFeedbackService, AnswerFeedbackService>();
        services.AddScoped<IChatHistoryService, ChatHistoryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAdminSupportService, AdminSupportService>();
        return services;
    }

    /// <summary>
    /// Registers all Application-layer services. Use in the API host.
    /// Equivalent to calling AddApplicationCore() followed by AddApplicationApiFeatures().
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddApplicationCore();
        services.AddApplicationApiFeatures();
        return services;
    }
}

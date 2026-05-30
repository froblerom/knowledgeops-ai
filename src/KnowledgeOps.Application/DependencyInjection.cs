using KnowledgeOps.Application.Auth.Commands;
using KnowledgeOps.Application.Auth.Queries;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Authorization.Hooks;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Application.Chat.Citations;
using KnowledgeOps.Application.Chat.Feedback;
using KnowledgeOps.Application.Chat.Prompting;
using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Retrieval;
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
        services.AddScoped<IEligibleSemanticRetrievalService, EligibleSemanticRetrievalService>();
        services.AddSingleton<IPromptAuthorizationFilter, DefaultPromptAuthorizationFilter>();
        services.AddSingleton<ICitationAuthorizationFilter, DefaultCitationAuthorizationFilter>();
        services.AddScoped<IGroundedPromptBuilder, GroundedPromptBuilder>();
        services.AddScoped<IContextSufficiencyPolicy, ContextSufficiencyPolicy>();
        services.AddScoped<ICitationMapper, CitationMapper>();
        services.AddScoped<IRagChatOrchestrationService, RagChatOrchestrationService>();
        services.AddScoped<IAnswerFeedbackService, AnswerFeedbackService>();

        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddSingleton<IOrganizationScopeService, OrganizationScopeService>();

        return services;
    }
}

using KnowledgeOps.Application.Auth.Commands;
using KnowledgeOps.Application.Auth.Queries;
using KnowledgeOps.Application.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeOps.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<GetCurrentUserQueryHandler>();

        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddSingleton<IOrganizationScopeService, OrganizationScopeService>();

        return services;
    }
}

using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Domain.Users;

namespace KnowledgeOps.Api.Tests.Support;

public sealed class AccessStateOverrides
{
    public bool DisableAdmins { get; set; }
    public bool DemoteAdmins { get; set; }
    public Guid? OrganizationIdOverride { get; set; }

    public void Reset()
    {
        DisableAdmins = false;
        DemoteAdmins = false;
        OrganizationIdOverride = null;
    }
}

public sealed class RepositoryUserAccessStateReader(
    IUserAuthRepository repository,
    AccessStateOverrides overrides) : IUserAccessStateReader
{
    public async Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await repository.FindByIdAsync(userId, ct);
        if (user is null || user.Status != UserStatus.Active)
            return null;

        if (overrides.DisableAdmins && user.Roles.Contains("Admin", StringComparer.Ordinal))
            return null;

        var roles = overrides.DemoteAdmins && user.Roles.Contains("Admin", StringComparer.Ordinal)
            ? new[] { "Agent" }
            : user.Roles;

        return new UserAccessState(user.UserId, overrides.OrganizationIdOverride ?? user.OrganizationId, roles);
    }
}

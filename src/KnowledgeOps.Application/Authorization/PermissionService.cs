using KnowledgeOps.Application.Auth.Abstractions;

namespace KnowledgeOps.Application.Authorization;

// Pure in-memory implementation. No EF Core or provider SDK dependencies.
public sealed class PermissionService : IPermissionService
{
    public bool HasPermission(ICurrentUser currentUser, string permission)
    {
        if (!currentUser.IsAuthenticated)
            return false;

        if (currentUser.Roles.Count == 0)
            return false;

        return RolePermissionMatrix.HasPermission(currentUser.Roles, permission);
    }

    public bool HasPermission(UserAccessState currentUser, string permission) =>
        RolePermissionMatrix.HasPermission(currentUser.Roles, permission);
}

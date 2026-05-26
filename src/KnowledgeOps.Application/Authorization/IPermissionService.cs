using KnowledgeOps.Application.Auth.Abstractions;

namespace KnowledgeOps.Application.Authorization;

public interface IPermissionService
{
    bool HasPermission(ICurrentUser currentUser, string permission);
}

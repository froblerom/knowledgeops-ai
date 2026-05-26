using Microsoft.AspNetCore.Authorization;

namespace KnowledgeOps.Api.Authorization;

// API transport adapter. Application owns the permission semantics and role-permission matrix.
// This attribute maps a permission name to an ASP.NET Core authorization policy.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public RequirePermissionAttribute(string permission)
        : base($"{PolicyPrefix}{permission}")
    {
        Permission = permission;
    }

    public string Permission { get; }
}

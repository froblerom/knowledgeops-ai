using System.ComponentModel.DataAnnotations;

namespace KnowledgeOps.Api.Controllers.Models;

public sealed class CreateUserRequest
{
    [Required, MaxLength(200)]
    public string DisplayName { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    public Guid? OrganizationId { get; init; }

    [Required]
    public string Status { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];

    [Required]
    public string InitialPassword { get; init; } = string.Empty;
}

public sealed class UpdateUserRequest
{
    [Required, MaxLength(200)]
    public string DisplayName { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    public Guid? OrganizationId { get; init; }

    [Required]
    public string Status { get; init; } = string.Empty;
}

public sealed class AssignRoleRequest
{
    [Required]
    public string RoleName { get; init; } = string.Empty;
}

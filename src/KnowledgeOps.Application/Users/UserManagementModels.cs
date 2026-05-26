using KnowledgeOps.Domain.Users;

namespace KnowledgeOps.Application.Users;

public sealed record UserManagementActor(Guid UserId, Guid OrganizationId);

public sealed record ManagedUser(
    Guid UserId,
    string DisplayName,
    string Email,
    Guid OrganizationId,
    UserStatus Status,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastLoginAt);

public sealed record CreateManagedUserCommand(
    string DisplayName,
    string Email,
    Guid? OrganizationId,
    string Status,
    IReadOnlyList<string> Roles,
    string InitialPassword);

public sealed record UpdateManagedUserCommand(
    string DisplayName,
    string Email,
    Guid? OrganizationId,
    string Status);

public sealed record NewManagedUser(
    Guid UserId,
    Guid OrganizationId,
    string DisplayName,
    string Email,
    string PasswordHash,
    UserStatus Status,
    IReadOnlyList<UserRoleName> Roles,
    Guid AssignedByUserId,
    DateTimeOffset CreatedAt);

public sealed record ManagedUserUpdate(
    string DisplayName,
    string Email,
    UserStatus Status,
    DateTimeOffset UpdatedAt);

public enum UserWriteOutcome
{
    Applied,
    NoChange,
    NotFound,
    Conflict,
    FinalActiveAdmin
}

public sealed record UserWriteResult(
    UserWriteOutcome Outcome,
    ManagedUser? User = null,
    bool StatusChanged = false);

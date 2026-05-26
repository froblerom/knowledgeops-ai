using KnowledgeOps.Domain.Users;

namespace KnowledgeOps.Application.Auth.Abstractions;

public interface IUserAuthRepository
{
    Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default);
    Task UpdateLastLoginAtAsync(Guid userId, DateTimeOffset loginAt, CancellationToken ct = default);
}

public sealed record UserAuthRecord(
    Guid UserId,
    Guid OrganizationId,
    string Email,
    string DisplayName,
    string? PasswordHash,
    UserStatus Status,
    IReadOnlyList<string> Roles);

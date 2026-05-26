using KnowledgeOps.Domain.Users;

namespace KnowledgeOps.Application.Users;

public interface IUserManagementRepository
{
    Task<IReadOnlyList<ManagedUser>> ListAsync(Guid organizationId, CancellationToken ct = default);
    Task<ManagedUser?> FindAsync(Guid userId, Guid organizationId, CancellationToken ct = default);
    Task<ManagedUser> CreateAsync(NewManagedUser user, CancellationToken ct = default);
    Task<UserWriteResult> UpdateAsync(
        Guid userId,
        Guid organizationId,
        ManagedUserUpdate update,
        CancellationToken ct = default);
    Task<UserWriteResult> AddRoleAsync(
        Guid userId,
        Guid organizationId,
        UserRoleName role,
        Guid assignedByUserId,
        DateTimeOffset assignedAt,
        CancellationToken ct = default);
    Task<UserWriteResult> RemoveRoleAsync(
        Guid userId,
        Guid organizationId,
        UserRoleName role,
        CancellationToken ct = default);
}

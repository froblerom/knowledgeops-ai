using System.Data;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Users;
using KnowledgeOps.Domain.Users;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Users;

internal sealed class EfUserManagementRepository(KnowledgeOpsDbContext dbContext) : IUserManagementRepository
{
    public async Task<IReadOnlyList<ManagedUser>> ListAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.OrganizationId == organizationId && user.DeletedAt == null)
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .ToListAsync(ct);

        return await MapManyAsync(users, ct);
    }

    public async Task<ManagedUser?> FindAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == userId
                    && candidate.OrganizationId == organizationId
                    && candidate.DeletedAt == null,
                ct);

        return user is null ? null : await MapAsync(user, ct);
    }

    public async Task<ManagedUser> CreateAsync(NewManagedUser newUser, CancellationToken ct = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        if (await dbContext.Users.AnyAsync(user => user.Email == newUser.Email, ct))
            throw new ApplicationConflictException();

        var user = new User
        {
            Id = newUser.UserId,
            OrganizationId = newUser.OrganizationId,
            DisplayName = newUser.DisplayName,
            Email = newUser.Email,
            PasswordHash = newUser.PasswordHash,
            Status = newUser.Status,
            CreatedAt = newUser.CreatedAt,
            UpdatedAt = newUser.CreatedAt
        };

        dbContext.Users.Add(user);
        foreach (var role in newUser.Roles)
        {
            dbContext.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleName = role,
                AssignedAt = newUser.CreatedAt,
                AssignedByUserId = newUser.AssignedByUserId
            });
        }

        try
        {
            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ApplicationConflictException();
        }

        return await MapAsync(user, ct);
    }

    public async Task<UserWriteResult> UpdateAsync(
        Guid userId,
        Guid organizationId,
        ManagedUserUpdate update,
        CancellationToken ct = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId
                && candidate.OrganizationId == organizationId
                && candidate.DeletedAt == null,
            ct);
        if (user is null)
            return new UserWriteResult(UserWriteOutcome.NotFound);

        if (await dbContext.Users.AnyAsync(
            candidate => candidate.Id != userId && candidate.Email == update.Email,
            ct))
        {
            return new UserWriteResult(UserWriteOutcome.Conflict);
        }

        var statusChanged = user.Status != update.Status;
        if (user.Status == UserStatus.Active
            && update.Status != UserStatus.Active
            && await IsAdminAsync(userId, ct)
            && await CountActiveAdminsAsync(organizationId, ct) <= 1)
        {
            return new UserWriteResult(UserWriteOutcome.FinalActiveAdmin);
        }

        var noChange = user.DisplayName == update.DisplayName
            && user.Email == update.Email
            && !statusChanged;
        if (noChange)
            return new UserWriteResult(UserWriteOutcome.NoChange, await MapAsync(user, ct));

        user.DisplayName = update.DisplayName;
        user.Email = update.Email;
        user.Status = update.Status;
        user.UpdatedAt = update.UpdatedAt;

        try
        {
            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            return new UserWriteResult(UserWriteOutcome.Conflict);
        }

        return new UserWriteResult(UserWriteOutcome.Applied, await MapAsync(user, ct), statusChanged);
    }

    public async Task<UserWriteResult> AddRoleAsync(
        Guid userId,
        Guid organizationId,
        UserRoleName role,
        Guid assignedByUserId,
        DateTimeOffset assignedAt,
        CancellationToken ct = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        var user = await ScopedUserAsync(userId, organizationId, ct);
        if (user is null)
            return new UserWriteResult(UserWriteOutcome.NotFound);

        if (await dbContext.UserRoles.AnyAsync(
            assignment => assignment.UserId == userId && assignment.RoleName == role,
            ct))
        {
            return new UserWriteResult(UserWriteOutcome.NoChange, await MapAsync(user, ct));
        }

        dbContext.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleName = role,
            AssignedAt = assignedAt,
            AssignedByUserId = assignedByUserId
        });
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new UserWriteResult(UserWriteOutcome.Applied, await MapAsync(user, ct));
    }

    public async Task<UserWriteResult> RemoveRoleAsync(
        Guid userId,
        Guid organizationId,
        UserRoleName role,
        CancellationToken ct = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        var user = await ScopedUserAsync(userId, organizationId, ct);
        if (user is null)
            return new UserWriteResult(UserWriteOutcome.NotFound);

        var assignment = await dbContext.UserRoles.SingleOrDefaultAsync(
            candidate => candidate.UserId == userId && candidate.RoleName == role,
            ct);
        if (assignment is null)
            return new UserWriteResult(UserWriteOutcome.NoChange, await MapAsync(user, ct));

        if (role == UserRoleName.Admin
            && user.Status == UserStatus.Active
            && await CountActiveAdminsAsync(organizationId, ct) <= 1)
        {
            return new UserWriteResult(UserWriteOutcome.FinalActiveAdmin);
        }

        dbContext.UserRoles.Remove(assignment);
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new UserWriteResult(UserWriteOutcome.Applied, await MapAsync(user, ct));
    }

    private Task<User?> ScopedUserAsync(Guid userId, Guid organizationId, CancellationToken ct) =>
        dbContext.Users.SingleOrDefaultAsync(
            user => user.Id == userId
                && user.OrganizationId == organizationId
                && user.DeletedAt == null,
            ct);

    private Task<bool> IsAdminAsync(Guid userId, CancellationToken ct) =>
        dbContext.UserRoles.AnyAsync(
            role => role.UserId == userId && role.RoleName == UserRoleName.Admin,
            ct);

    private Task<int> CountActiveAdminsAsync(Guid organizationId, CancellationToken ct) =>
        dbContext.Users
            .Where(user =>
                user.OrganizationId == organizationId
                && user.DeletedAt == null
                && user.Status == UserStatus.Active)
            .Join(
                dbContext.UserRoles.Where(role => role.RoleName == UserRoleName.Admin),
                user => user.Id,
                role => role.UserId,
                (user, _) => user.Id)
            .Distinct()
            .CountAsync(ct);

    private async Task<IReadOnlyList<ManagedUser>> MapManyAsync(
        IReadOnlyList<User> users,
        CancellationToken ct)
    {
        if (users.Count == 0)
            return [];

        var ids = users.Select(user => user.Id).ToArray();
        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Where(role => ids.Contains(role.UserId))
            .GroupBy(role => role.UserId)
            .ToDictionaryAsync(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .OrderBy(role => role.RoleName)
                    .Select(role => role.RoleName.ToString())
                    .ToList(),
                ct);

        return users.Select(user => Map(user, roles.GetValueOrDefault(user.Id, []))).ToArray();
    }

    private async Task<ManagedUser> MapAsync(User user, CancellationToken ct)
    {
        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Where(role => role.UserId == user.Id)
            .OrderBy(role => role.RoleName)
            .Select(role => role.RoleName.ToString())
            .ToListAsync(ct);
        return Map(user, roles);
    }

    private static ManagedUser Map(User user, IReadOnlyList<string> roles) =>
        new(
            user.Id,
            user.DisplayName,
            user.Email,
            user.OrganizationId,
            user.Status,
            roles,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt);
}

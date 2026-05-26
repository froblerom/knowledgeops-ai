using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Domain.Users;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Auth;

internal sealed class UserAuthRepository : IUserAuthRepository
{
    private readonly KnowledgeOpsDbContext _context;

    public UserAuthRepository(KnowledgeOpsDbContext context)
    {
        _context = context;
    }

    public async Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Email == email && u.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (user is null) return null;

        var roles = await LoadRolesAsync(user.Id, ct);
        return ToRecord(user, roles);
    }

    public async Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (user is null) return null;

        var roles = await LoadRolesAsync(user.Id, ct);
        return ToRecord(user, roles);
    }

    public async Task UpdateLastLoginAtAsync(Guid userId, DateTimeOffset loginAt, CancellationToken ct = default)
    {
        await _context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.LastLoginAt, loginAt)
                    .SetProperty(u => u.UpdatedAt, loginAt),
                ct);
    }

    private async Task<IReadOnlyList<string>> LoadRolesAsync(Guid userId, CancellationToken ct)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => r.RoleName.ToString())
            .ToListAsync(ct);
    }

    private static UserAuthRecord ToRecord(User user, IReadOnlyList<string> roles)
    {
        return new UserAuthRecord(
            user.Id,
            user.OrganizationId,
            user.Email,
            user.DisplayName,
            user.PasswordHash,
            user.Status,
            roles);
    }
}

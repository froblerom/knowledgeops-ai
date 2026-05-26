using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Domain.Users;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Authorization;

internal sealed class EfUserAccessStateReader(KnowledgeOpsDbContext dbContext) : IUserAccessStateReader
{
    public async Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(candidate =>
                candidate.Id == userId
                && candidate.DeletedAt == null
                && candidate.Status == UserStatus.Active)
            .Select(candidate => new { candidate.Id, candidate.OrganizationId })
            .SingleOrDefaultAsync(ct);

        if (user is null)
            return null;

        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Where(role => role.UserId == user.Id)
            .Select(role => role.RoleName.ToString())
            .ToListAsync(ct);

        return new UserAccessState(user.Id, user.OrganizationId, roles);
    }
}

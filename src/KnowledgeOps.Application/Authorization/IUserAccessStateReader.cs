namespace KnowledgeOps.Application.Authorization;

public interface IUserAccessStateReader
{
    Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default);
}

public sealed record UserAccessState(
    Guid UserId,
    Guid OrganizationId,
    IReadOnlyList<string> Roles);

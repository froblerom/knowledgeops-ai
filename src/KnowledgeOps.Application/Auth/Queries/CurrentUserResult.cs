namespace KnowledgeOps.Application.Auth.Queries;

public sealed record CurrentUserResult(
    Guid UserId,
    string Email,
    string DisplayName,
    Guid OrganizationId,
    IReadOnlyList<string> Roles,
    string Status);

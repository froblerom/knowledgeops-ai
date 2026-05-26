namespace KnowledgeOps.Application.Auth.Commands;

public sealed record LoginResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    string DisplayName,
    Guid OrganizationId,
    IReadOnlyList<string> Roles);

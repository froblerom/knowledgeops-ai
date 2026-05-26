namespace KnowledgeOps.Application.Auth.Abstractions;

public interface ITokenService
{
    TokenResult IssueToken(
        Guid userId,
        Guid organizationId,
        string email,
        string displayName,
        IReadOnlyList<string> roles);
}

public sealed record TokenResult(string AccessToken, DateTimeOffset ExpiresAt);

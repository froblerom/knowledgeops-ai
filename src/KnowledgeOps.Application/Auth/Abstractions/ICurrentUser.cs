namespace KnowledgeOps.Application.Auth.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
    Guid OrganizationId { get; }
    string Email { get; }
    string DisplayName { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}

namespace KnowledgeOps.Api.Controllers.Models;

public sealed class UserResponse
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
}

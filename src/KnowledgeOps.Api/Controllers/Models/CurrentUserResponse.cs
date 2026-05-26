namespace KnowledgeOps.Api.Controllers.Models;

public sealed class CurrentUserResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public string Status { get; set; } = string.Empty;
}

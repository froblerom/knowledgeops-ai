namespace KnowledgeOps.Api.Controllers.Models;

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public CurrentUserResponse User { get; set; } = new();
}

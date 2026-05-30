namespace KnowledgeOps.Domain.Chat;

public sealed class ChatSession
{
    public const string StatusActive = "Active";

    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public string? Title { get; init; }
    public string Status { get; private set; } = StatusActive;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset? LastInteractionAt { get; private set; }

    public static ChatSession Create(Guid orgId, Guid userId, string? title)
    {
        var now = DateTimeOffset.UtcNow;
        return new ChatSession
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            UserId = userId,
            Title = title,
            Status = StatusActive,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void UpdateLastInteractionAt(DateTimeOffset at)
    {
        LastInteractionAt = at;
        UpdatedAt = at;
    }

    public bool IsOwnedBy(Guid userId, Guid organizationId) =>
        UserId == userId && OrganizationId == organizationId;
}

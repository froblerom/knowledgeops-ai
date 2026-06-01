namespace KnowledgeOps.Domain.Chat;

public sealed class AnswerFeedback
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public Guid ChatInteractionId { get; init; }
    public AnswerFeedbackRating Rating { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static AnswerFeedback Create(
        Guid organizationId,
        Guid userId,
        Guid chatInteractionId,
        AnswerFeedbackRating rating)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));
        if (chatInteractionId == Guid.Empty)
            throw new ArgumentException("Chat interaction id is required.", nameof(chatInteractionId));

        var now = DateTimeOffset.UtcNow;
        return new AnswerFeedback
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            ChatInteractionId = chatInteractionId,
            Rating = rating,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void UpdateRating(AnswerFeedbackRating rating)
    {
        Rating = rating;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

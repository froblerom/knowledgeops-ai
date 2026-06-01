namespace KnowledgeOps.Api.Controllers.Models;

public sealed class AnswerFeedbackRatingRequest
{
    public string Rating { get; init; } = string.Empty;
}

public sealed record AnswerFeedbackResponse(
    Guid FeedbackId,
    Guid ChatInteractionId,
    Guid UserId,
    string Rating,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AnswerFeedbackReviewResponse(
    int UsefulCount,
    int NotUsefulCount,
    IReadOnlyList<AnswerFeedbackReviewItemResponse> Items);

public sealed record AnswerFeedbackReviewItemResponse(
    Guid FeedbackId,
    Guid ChatInteractionId,
    Guid UserId,
    string Rating,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

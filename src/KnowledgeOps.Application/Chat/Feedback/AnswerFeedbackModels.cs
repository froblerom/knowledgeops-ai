using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Application.Chat.Feedback;

public sealed record SubmitAnswerFeedbackRequest(
    Guid ChatInteractionId,
    AnswerFeedbackRating Rating);

public sealed record UpdateAnswerFeedbackRequest(
    Guid ChatInteractionId,
    AnswerFeedbackRating Rating);

public sealed record AnswerFeedbackResult(
    Guid FeedbackId,
    Guid ChatInteractionId,
    Guid UserId,
    Guid OrganizationId,
    AnswerFeedbackRating Rating,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AnswerFeedbackReviewItem(
    Guid FeedbackId,
    Guid ChatInteractionId,
    Guid UserId,
    AnswerFeedbackRating Rating,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AnswerFeedbackReviewResult(
    int UsefulCount,
    int NotUsefulCount,
    IReadOnlyList<AnswerFeedbackReviewItem> Items);

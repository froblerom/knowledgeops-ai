namespace KnowledgeOps.Application.Chat.Feedback;

public interface IAnswerFeedbackService
{
    Task<AnswerFeedbackResult> SubmitAsync(SubmitAnswerFeedbackRequest request, CancellationToken ct = default);
    Task<AnswerFeedbackResult> UpdateOwnAsync(UpdateAnswerFeedbackRequest request, CancellationToken ct = default);
    Task<AnswerFeedbackReviewResult> GetReviewDataAsync(CancellationToken ct = default);
}

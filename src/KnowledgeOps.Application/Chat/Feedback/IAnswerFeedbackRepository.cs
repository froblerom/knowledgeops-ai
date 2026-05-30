using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Application.Chat.Feedback;

public interface IAnswerFeedbackRepository
{
    Task<ChatInteraction?> FindInteractionAsync(Guid interactionId, Guid organizationId, CancellationToken ct = default);
    Task<AnswerFeedback?> FindByIdAsync(Guid feedbackId, Guid organizationId, CancellationToken ct = default);
    Task<AnswerFeedback?> FindByInteractionAndUserAsync(
        Guid interactionId,
        Guid userId,
        Guid organizationId,
        CancellationToken ct = default);

    Task<IReadOnlyList<AnswerFeedback>> ListForReviewAsync(Guid organizationId, CancellationToken ct = default);
    Task AddAsync(AnswerFeedback feedback, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

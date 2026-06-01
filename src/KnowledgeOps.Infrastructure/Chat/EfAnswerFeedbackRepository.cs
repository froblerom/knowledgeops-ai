using KnowledgeOps.Application.Chat.Feedback;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class EfAnswerFeedbackRepository(KnowledgeOpsDbContext db) : IAnswerFeedbackRepository
{
    public Task<ChatInteraction?> FindInteractionAsync(
        Guid interactionId,
        Guid organizationId,
        CancellationToken ct = default) =>
        db.ChatInteractions.FirstOrDefaultAsync(
            i => i.Id == interactionId && i.OrganizationId == organizationId,
            ct);

    public Task<AnswerFeedback?> FindByIdAsync(
        Guid feedbackId,
        Guid organizationId,
        CancellationToken ct = default) =>
        db.AnswerFeedback.FirstOrDefaultAsync(
            f => f.Id == feedbackId && f.OrganizationId == organizationId,
            ct);

    public Task<AnswerFeedback?> FindByInteractionAndUserAsync(
        Guid interactionId,
        Guid userId,
        Guid organizationId,
        CancellationToken ct = default) =>
        db.AnswerFeedback.FirstOrDefaultAsync(
            f => f.ChatInteractionId == interactionId
                && f.UserId == userId
                && f.OrganizationId == organizationId,
            ct);

    public async Task<IReadOnlyList<AnswerFeedback>> ListForReviewAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        await db.AnswerFeedback
            .Where(f => f.OrganizationId == organizationId)
            .ToArrayAsync(ct);

    public async Task AddAsync(AnswerFeedback feedback, CancellationToken ct = default) =>
        await db.AnswerFeedback.AddAsync(feedback, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

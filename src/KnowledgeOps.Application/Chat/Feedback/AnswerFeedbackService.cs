using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Chat;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Chat.Feedback;

internal sealed class AnswerFeedbackService(
    ICurrentUser currentUser,
    IUserAccessStateReader accessStateReader,
    IPermissionService permissionService,
    IAnswerFeedbackRepository repository,
    IAuditEventWriter auditWriter,
    ICorrelationContext correlationContext,
    ILogger<AnswerFeedbackService> logger) : IAnswerFeedbackService
{
    public async Task<AnswerFeedbackResult> SubmitAsync(
        SubmitAnswerFeedbackRequest request,
        CancellationToken ct = default)
    {
        if (request.ChatInteractionId == Guid.Empty)
            throw Validation(nameof(request.ChatInteractionId), "Chat interaction id is required.");

        var state = await RequireActiveStateAsync(ct);
        RequirePermission(state, KnowledgeOpsPermissions.Feedback.Submit);

        var interaction = await repository.FindInteractionAsync(
            request.ChatInteractionId,
            state.OrganizationId,
            ct);

        if (interaction is null || !CanAccessInteraction(interaction, state))
            throw new ApplicationNotFoundException();

        var existing = await repository.FindByInteractionAndUserAsync(
            request.ChatInteractionId,
            state.UserId,
            state.OrganizationId,
            ct);

        if (existing is not null)
            throw new ApplicationConflictException();

        var feedback = AnswerFeedback.Create(
            state.OrganizationId,
            state.UserId,
            request.ChatInteractionId,
            request.Rating);

        await repository.AddAsync(feedback, ct);
        await repository.SaveChangesAsync(ct);

        await AuditAsync(
            AuditEventTypes.FeedbackSubmitted,
            $"Feedback submitted. InteractionId={feedback.ChatInteractionId} Rating={feedback.Rating}",
            feedback,
            ct);

        return ToResult(feedback);
    }

    public async Task<AnswerFeedbackResult> UpdateOwnAsync(
        UpdateAnswerFeedbackRequest request,
        CancellationToken ct = default)
    {
        if (request.ChatInteractionId == Guid.Empty)
            throw Validation(nameof(request.ChatInteractionId), "Chat interaction id is required.");

        var state = await RequireActiveStateAsync(ct);
        RequirePermission(state, KnowledgeOpsPermissions.Feedback.UpdateOwn);

        var interaction = await repository.FindInteractionAsync(
            request.ChatInteractionId,
            state.OrganizationId,
            ct);

        if (interaction is null || !CanAccessInteraction(interaction, state))
            throw new ApplicationNotFoundException();

        var feedback = await repository.FindByInteractionAndUserAsync(
            request.ChatInteractionId,
            state.UserId,
            state.OrganizationId,
            ct);

        if (feedback is null)
            throw new ApplicationNotFoundException();

        if (feedback.UserId != state.UserId)
            throw new ApplicationForbiddenException();

        feedback.UpdateRating(request.Rating);
        await repository.SaveChangesAsync(ct);

        await AuditAsync(
            AuditEventTypes.FeedbackUpdated,
            $"Feedback updated. InteractionId={feedback.ChatInteractionId} Rating={feedback.Rating}",
            feedback,
            ct);

        return ToResult(feedback);
    }

    public async Task<AnswerFeedbackReviewResult> GetReviewDataAsync(CancellationToken ct = default)
    {
        var state = await RequireActiveStateAsync(ct);
        RequirePermission(state, KnowledgeOpsPermissions.Feedback.ViewReviewData);

        var feedback = await repository.ListForReviewAsync(state.OrganizationId, ct);
        var useful = feedback.Count(f => f.Rating == AnswerFeedbackRating.Useful);
        var notUseful = feedback.Count(f => f.Rating == AnswerFeedbackRating.NotUseful);

        return new AnswerFeedbackReviewResult(
            useful,
            notUseful,
            feedback
                .OrderByDescending(f => f.UpdatedAt)
                .Select(f => new AnswerFeedbackReviewItem(
                    f.Id,
                    f.ChatInteractionId,
                    f.UserId,
                    f.Rating,
                    f.CreatedAt,
                    f.UpdatedAt))
                .ToArray());
    }

    private async Task<UserAccessState> RequireActiveStateAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            throw new ApplicationUnauthenticatedException();

        var state = await accessStateReader.FindActiveByIdAsync(currentUser.UserId, ct);
        if (state is null)
            throw new ApplicationUnauthenticatedException();

        if (state.OrganizationId == Guid.Empty)
            throw new ApplicationForbiddenException();

        return state;
    }

    private void RequirePermission(UserAccessState state, string permission)
    {
        if (!permissionService.HasPermission(state, permission))
            throw new ApplicationForbiddenException();
    }

    private bool CanAccessInteraction(ChatInteraction interaction, UserAccessState state) =>
        interaction.OrganizationId == state.OrganizationId
        && (interaction.UserId == state.UserId
            || permissionService.HasPermission(state, KnowledgeOpsPermissions.Chat.ViewScopedHistory));

    private async Task AuditAsync(
        string eventType,
        string message,
        AnswerFeedback feedback,
        CancellationToken ct)
    {
        try
        {
            await auditWriter.WriteAsync(
                new AuditEvent(
                    eventType,
                    message,
                    AuditSeverity.Info,
                    correlationContext.CorrelationId,
                    feedback.OrganizationId,
                    feedback.UserId,
                    "Feedback",
                    feedback.Id),
                ct);
        }
        catch
        {
            logger.LogWarning(
                "Feedback audit write failed. EventType={EventType} CorrelationId={CorrelationId}",
                eventType,
                correlationContext.CorrelationId);
        }
    }

    private static AnswerFeedbackResult ToResult(AnswerFeedback feedback) =>
        new(
            feedback.Id,
            feedback.ChatInteractionId,
            feedback.UserId,
            feedback.OrganizationId,
            feedback.Rating,
            feedback.CreatedAt,
            feedback.UpdatedAt);

    private static ApplicationValidationException Validation(string field, string message) =>
        new([new ApplicationValidationItem(field, message)]);
}

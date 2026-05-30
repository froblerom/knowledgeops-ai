using KnowledgeOps.Api.Authorization;
using KnowledgeOps.Api.Controllers.Models;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Domain.Chat;
using Microsoft.AspNetCore.Mvc;
using FeedbackResult = KnowledgeOps.Application.Chat.Feedback.AnswerFeedbackResult;
using FeedbackService = KnowledgeOps.Application.Chat.Feedback.IAnswerFeedbackService;
using SubmitFeedbackCommand = KnowledgeOps.Application.Chat.Feedback.SubmitAnswerFeedbackRequest;
using UpdateFeedbackCommand = KnowledgeOps.Application.Chat.Feedback.UpdateAnswerFeedbackRequest;

namespace KnowledgeOps.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class FeedbackController(FeedbackService service) : ControllerBase
{
    [HttpPost("chat/interactions/{chatInteractionId:guid}/feedback")]
    [RequirePermission(KnowledgeOpsPermissions.Feedback.Submit)]
    public async Task<ActionResult<AnswerFeedbackResponse>> Submit(
        Guid chatInteractionId,
        [FromBody] AnswerFeedbackRatingRequest request,
        CancellationToken ct)
    {
        var result = await service.SubmitAsync(
            new SubmitFeedbackCommand(
                chatInteractionId,
                ParseRating(request.Rating)),
            ct);

        return Created($"/api/v1/chat/interactions/{chatInteractionId}/feedback", ToResponse(result));
    }

    [HttpPut("chat/interactions/{chatInteractionId:guid}/feedback")]
    [RequirePermission(KnowledgeOpsPermissions.Feedback.UpdateOwn)]
    public async Task<ActionResult<AnswerFeedbackResponse>> Update(
        Guid chatInteractionId,
        [FromBody] AnswerFeedbackRatingRequest request,
        CancellationToken ct)
    {
        var result = await service.UpdateOwnAsync(
            new UpdateFeedbackCommand(
                chatInteractionId,
                ParseRating(request.Rating)),
            ct);

        return Ok(ToResponse(result));
    }

    [HttpGet("feedback")]
    [RequirePermission(KnowledgeOpsPermissions.Feedback.ViewReviewData)]
    public async Task<ActionResult<AnswerFeedbackReviewResponse>> GetReviewData(CancellationToken ct)
    {
        var result = await service.GetReviewDataAsync(ct);
        return Ok(new AnswerFeedbackReviewResponse(
            result.UsefulCount,
            result.NotUsefulCount,
            result.Items.Select(i => new AnswerFeedbackReviewItemResponse(
                i.FeedbackId,
                i.ChatInteractionId,
                i.UserId,
                i.Rating.ToString(),
                i.CreatedAt,
                i.UpdatedAt)).ToArray()));
    }

    private static AnswerFeedbackResponse ToResponse(FeedbackResult result) =>
        new(
            result.FeedbackId,
            result.ChatInteractionId,
            result.UserId,
            result.Rating.ToString(),
            result.CreatedAt,
            result.UpdatedAt);

    private static AnswerFeedbackRating ParseRating(string? rating)
    {
        if (Enum.TryParse<AnswerFeedbackRating>(rating, ignoreCase: false, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new ApplicationValidationException(
            [new ApplicationValidationItem(nameof(AnswerFeedbackRatingRequest.Rating), "Rating must be Useful or NotUseful.")]);
    }
}

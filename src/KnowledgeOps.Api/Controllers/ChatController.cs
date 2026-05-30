using KnowledgeOps.Api.Authorization;
using KnowledgeOps.Api.Controllers.Models;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Chat;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Api.Controllers;

[ApiController]
[Route("api/v1/chat")]
public sealed class ChatController(
    IRagChatOrchestrationService service,
    ICorrelationContext correlationContext) : ControllerBase
{
    [HttpPost("questions")]
    [RequirePermission(KnowledgeOpsPermissions.Chat.AskQuestion)]
    public async Task<ActionResult<ChatAskQuestionResponse>> AskQuestion(
        [FromBody] ChatAskQuestionRequest request,
        CancellationToken ct)
    {
        var questionText = request.QuestionText.Trim();
        if (string.IsNullOrWhiteSpace(questionText))
        {
            return BadRequest(ApiErrorResponses.Create(
                ApiErrorResponses.ValidationCode,
                "One or more validation errors occurred.",
                correlationContext.CorrelationId,
                [new ApiValidationItem(nameof(request.QuestionText), "Question text is required.")]));
        }

        var response = await service.AskAsync(
            new AskQuestionRequest(questionText, request.ChatSessionId),
            ct);

        return Ok(ToResponse(response));
    }

    private static ChatAskQuestionResponse ToResponse(AskQuestionResponse response)
    {
        var answerState = ToApiAnswerState(response.AnswerState);
        var citations = response.AnswerState == AnswerState.Grounded
            ? response.Citations?.Select(ToResponse).ToArray() ?? []
            : [];

        return new ChatAskQuestionResponse(
            response.ChatInteractionId,
            response.ChatSessionId,
            answerState,
            response.AnswerState == AnswerState.ProviderFailed ? null : response.AnswerText,
            response.IsInsufficientContext,
            citations,
            new ChatMetadataDto(
                LatencyMs: null,
                RetrievalResultCount: response.RetrievalCandidateCount,
                EstimatedCost: null),
            response.CorrelationId);
    }

    private static string ToApiAnswerState(AnswerState answerState) =>
        answerState switch
        {
            AnswerState.Grounded => "GroundedAnswer",
            AnswerState.InsufficientContext => "InsufficientContext",
            AnswerState.ProviderFailed => "ProviderFailure",
            _ => "ProviderFailure"
        };

    private static ChatCitationDto ToResponse(CitationResponse citation) =>
        new(
            citation.CitationId,
            citation.DocumentId,
            citation.DocumentTitle,
            citation.ChunkId,
            citation.PageNumber,
            citation.SectionLabel,
            citation.RelevanceScore,
            citation.Rank);
}

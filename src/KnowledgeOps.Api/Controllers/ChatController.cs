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
    IChatHistoryService historyService,
    ICorrelationContext correlationContext) : ControllerBase
{
    // ── Ask question ─────────────────────────────────────────────────────────

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

        return Ok(ToAskResponse(response));
    }

    // ── Chat session history ─────────────────────────────────────────────────

    [HttpGet("sessions")]
    [RequirePermission(KnowledgeOpsPermissions.Chat.ViewOwnHistory)]
    public async Task<ActionResult<IReadOnlyList<ChatSessionSummaryResponse>>> GetSessions(
        [FromQuery] bool scoped = false,
        CancellationToken ct = default)
    {
        var sessions = await historyService.GetSessionsAsync(scoped, ct);
        return Ok(sessions.Select(ToSessionSummaryResponse).ToList());
    }

    [HttpPost("sessions")]
    [RequirePermission(KnowledgeOpsPermissions.Chat.AskQuestion)]
    public async Task<ActionResult<CreateChatSessionResponse>> CreateSession(
        [FromBody] CreateChatSessionRequest request,
        CancellationToken ct)
    {
        var sessionId = await historyService.CreateSessionAsync(request.Title, ct);
        return Created($"/api/v1/chat/sessions/{sessionId}", new CreateChatSessionResponse(sessionId));
    }

    [HttpGet("sessions/{chatSessionId:guid}")]
    [RequirePermission(KnowledgeOpsPermissions.Chat.ViewOwnHistory)]
    public async Task<ActionResult<ChatSessionDetailResponse>> GetSession(
        Guid chatSessionId, CancellationToken ct)
    {
        var session = await historyService.GetSessionAsync(chatSessionId, ct);
        if (session is null)
            return NotFound();

        return Ok(ToSessionDetailResponse(session));
    }

    // ── Chat interaction history ─────────────────────────────────────────────

    [HttpGet("interactions/{chatInteractionId:guid}")]
    [RequirePermission(KnowledgeOpsPermissions.Chat.ViewInteraction)]
    public async Task<ActionResult<ChatInteractionDetailResponse>> GetInteraction(
        Guid chatInteractionId, CancellationToken ct)
    {
        var interaction = await historyService.GetInteractionAsync(chatInteractionId, ct);
        if (interaction is null)
            return NotFound();

        return Ok(ToInteractionDetailResponse(interaction));
    }

    [HttpGet("interactions/{chatInteractionId:guid}/citations")]
    [RequirePermission(KnowledgeOpsPermissions.Chat.ViewCitations)]
    public async Task<ActionResult<IReadOnlyList<ChatCitationHistoryResponse>>> GetInteractionCitations(
        Guid chatInteractionId, CancellationToken ct)
    {
        var citations = await historyService.GetInteractionCitationsAsync(chatInteractionId, ct);
        if (citations is null)
            return NotFound();

        return Ok(citations.Select(ToCitationHistoryResponse).ToList());
    }

    // ── Mapping helpers ──────────────────────────────────────────────────────

    private static ChatAskQuestionResponse ToAskResponse(AskQuestionResponse response)
    {
        var answerState = ToApiAnswerState(response.AnswerState);
        var citations = response.AnswerState == AnswerState.Grounded
            ? response.Citations?.Select(ToCitationDto).ToArray() ?? []
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

    private static ChatCitationDto ToCitationDto(CitationResponse citation) =>
        new(citation.CitationId, citation.DocumentId, citation.DocumentTitle,
            citation.ChunkId, citation.PageNumber, citation.SectionLabel,
            citation.RelevanceScore, citation.Rank);

    private static ChatSessionSummaryResponse ToSessionSummaryResponse(ChatSessionSummaryDto dto) =>
        new(dto.ChatSessionId, dto.Title, dto.Status, dto.CreatedAt, dto.UpdatedAt,
            dto.LastInteractionAt, dto.InteractionCount);

    private static ChatSessionDetailResponse ToSessionDetailResponse(ChatSessionDetailDto dto) =>
        new(dto.ChatSessionId, dto.Title, dto.Status, dto.CreatedAt, dto.UpdatedAt,
            dto.LastInteractionAt,
            dto.Interactions.Select(i => new ChatInteractionSummaryResponse(
                i.ChatInteractionId, i.AnswerState, i.InsufficientContext, i.CreatedAt)).ToList());

    private static ChatInteractionDetailResponse ToInteractionDetailResponse(ChatInteractionDetailDto dto) =>
        new(dto.ChatInteractionId, dto.ChatSessionId, dto.AnswerState, dto.InsufficientContext,
            dto.QuestionText, dto.AnswerText, dto.PromptVersion, dto.CorrelationId,
            new ChatInteractionMetadataResponse(
                dto.Metadata.RetrievalCandidateCount,
                dto.Metadata.RetrievalLatencyMs,
                dto.Metadata.GenerationLatencyMs,
                dto.Metadata.TotalLatencyMs,
                dto.Metadata.TokenUsageInput,
                dto.Metadata.TokenUsageOutput,
                dto.Metadata.EstimatedCost),
            dto.Citations.Select(ToCitationHistoryResponse).ToList(),
            dto.CreatedAt);

    private static ChatCitationHistoryResponse ToCitationHistoryResponse(ChatCitationHistoryDto dto) =>
        new(dto.CitationId, dto.ChatInteractionId, dto.DocumentId, dto.ChunkId, dto.Rank,
            dto.DocumentTitle, dto.PageNumber, dto.SectionLabel, dto.RelevanceScore);
}

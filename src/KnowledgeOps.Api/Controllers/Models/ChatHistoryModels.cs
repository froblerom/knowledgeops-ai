using System.ComponentModel.DataAnnotations;

namespace KnowledgeOps.Api.Controllers.Models;

// ── Request models ────────────────────────────────────────────────────────────

public sealed class CreateChatSessionRequest
{
    [MaxLength(300)]
    public string? Title { get; init; }
}

// ── Response models ───────────────────────────────────────────────────────────

public sealed record ChatSessionSummaryResponse(
    Guid ChatSessionId,
    string? Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastInteractionAt,
    int InteractionCount);

public sealed record ChatSessionDetailResponse(
    Guid ChatSessionId,
    string? Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastInteractionAt,
    IReadOnlyList<ChatInteractionSummaryResponse> Interactions);

public sealed record ChatInteractionSummaryResponse(
    Guid ChatInteractionId,
    string AnswerState,
    bool InsufficientContext,
    DateTimeOffset CreatedAt);

public sealed record ChatInteractionDetailResponse(
    Guid ChatInteractionId,
    Guid ChatSessionId,
    string AnswerState,
    bool InsufficientContext,
    string? QuestionText,
    string? AnswerText,
    string? PromptVersion,
    string? CorrelationId,
    ChatInteractionMetadataResponse Metadata,
    IReadOnlyList<ChatCitationHistoryResponse> Citations,
    DateTimeOffset CreatedAt);

public sealed record ChatInteractionMetadataResponse(
    int RetrievalCandidateCount,
    long? RetrievalLatencyMs,
    long? GenerationLatencyMs,
    long? TotalLatencyMs,
    int? TokenUsageInput,
    int? TokenUsageOutput,
    decimal? EstimatedCost);

public sealed record ChatCitationHistoryResponse(
    Guid CitationId,
    Guid ChatInteractionId,
    Guid DocumentId,
    Guid ChunkId,
    int Rank,
    string DocumentTitle,
    int? PageNumber,
    string? SectionLabel,
    double? RelevanceScore);

public sealed record CreateChatSessionResponse(Guid ChatSessionId);

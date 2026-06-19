using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Application.Chat;

// ── Chat history read models ─────────────────────────────────────────────────

public sealed record ChatSessionSummaryDto(
    Guid ChatSessionId,
    string? Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastInteractionAt,
    int InteractionCount);

public sealed record ChatSessionDetailDto(
    Guid ChatSessionId,
    string? Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastInteractionAt,
    IReadOnlyList<ChatInteractionSummaryDto> Interactions);

public sealed record ChatInteractionSummaryDto(
    Guid ChatInteractionId,
    string AnswerState,
    bool InsufficientContext,
    DateTimeOffset CreatedAt);

public sealed record ChatInteractionDetailDto(
    Guid ChatInteractionId,
    Guid ChatSessionId,
    string AnswerState,
    bool InsufficientContext,
    string? QuestionText,
    string? AnswerText,
    string? PromptVersion,
    string? CorrelationId,
    ChatRetrievalMetadataDto Metadata,
    IReadOnlyList<ChatCitationHistoryDto> Citations,
    DateTimeOffset CreatedAt);

public sealed record ChatRetrievalMetadataDto(
    int RetrievalCandidateCount,
    long? RetrievalLatencyMs,
    long? GenerationLatencyMs,
    long? TotalLatencyMs,
    int? TokenUsageInput,
    int? TokenUsageOutput,
    decimal? EstimatedCost,
    string? AiProvider,
    string? AiModel,
    string? ProviderFailureCode);

public sealed record ChatCitationHistoryDto(
    Guid CitationId,
    Guid ChatInteractionId,
    Guid DocumentId,
    Guid ChunkId,
    int Rank,
    string DocumentTitle,
    int? PageNumber,
    string? SectionLabel,
    double? RelevanceScore);

// ── Chat history service ─────────────────────────────────────────────────────

public sealed record AnswerGenerationRequest(
    IReadOnlyList<AuthorizedChunkContext> AuthorizedChunks,
    string UserQuestion,
    string? PromptVersion = null,
    string? ModelName = null,
    string? SystemInstruction = null,
    string? FormattedContext = null);

public sealed record AuthorizedChunkContext(
    Guid ChunkId,
    Guid DocumentId,
    Guid OrganizationId,
    string ChunkText,
    int ChunkIndex,
    int? PageNumber = null,
    string? SectionLabel = null,
    double? RelevanceScore = null);

public sealed record AnswerGenerationResult(
    AnswerState State,
    string? AnswerText,
    int? InputTokens,
    int? OutputTokens,
    string? ModelUsed,
    string? ProviderName,
    string? SafeFailureCode);

public sealed record AskQuestionRequest(
    string QuestionText,
    Guid? ChatSessionId = null);

public sealed record CitationResponse(
    Guid CitationId,
    Guid DocumentId,
    Guid ChunkId,
    int Rank,
    string DocumentTitle,
    int? PageNumber,
    string? SectionLabel,
    double? RelevanceScore);

public sealed record AskQuestionResponse(
    Guid ChatInteractionId,
    Guid ChatSessionId,
    AnswerState AnswerState,
    string? AnswerText,
    int RetrievalCandidateCount,
    bool IsInsufficientContext,
    string CorrelationId,
    IReadOnlyList<CitationResponse>? Citations = null);

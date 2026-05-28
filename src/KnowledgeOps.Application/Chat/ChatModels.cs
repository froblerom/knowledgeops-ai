using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Application.Chat;

public sealed record AnswerGenerationRequest(
    IReadOnlyList<AuthorizedChunkContext> AuthorizedChunks,
    string UserQuestion,
    string? PromptVersion = null,
    string? ModelName = null);

public sealed record AuthorizedChunkContext(
    Guid ChunkId,
    Guid DocumentId,
    Guid OrganizationId,
    string ChunkText,
    int ChunkIndex,
    int? PageNumber = null,
    string? SectionLabel = null);

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

public sealed record AskQuestionResponse(
    Guid ChatInteractionId,
    Guid ChatSessionId,
    AnswerState AnswerState,
    string? AnswerText,
    int RetrievalCandidateCount,
    bool IsInsufficientContext,
    string CorrelationId);

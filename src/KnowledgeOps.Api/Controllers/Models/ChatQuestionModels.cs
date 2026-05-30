using System.ComponentModel.DataAnnotations;

namespace KnowledgeOps.Api.Controllers.Models;

public sealed class ChatAskQuestionRequest
{
    [Required]
    [MaxLength(4000)]
    public string QuestionText { get; init; } = string.Empty;

    public Guid? ChatSessionId { get; init; }
}

public sealed record ChatAskQuestionResponse(
    Guid ChatInteractionId,
    Guid ChatSessionId,
    string AnswerState,
    string? Answer,
    bool InsufficientContext,
    IReadOnlyList<ChatCitationDto> Citations,
    ChatMetadataDto Metadata,
    string? CorrelationId);

public sealed record ChatCitationDto(
    Guid CitationId,
    Guid DocumentId,
    string? DocumentTitle,
    Guid ChunkId,
    int? PageNumber,
    string? SectionLabel,
    double? Score,
    int Rank);

public sealed record ChatMetadataDto(
    long? LatencyMs,
    int RetrievalResultCount,
    decimal? EstimatedCost);

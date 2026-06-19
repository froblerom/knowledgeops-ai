namespace KnowledgeOps.Domain.Chat;

public sealed class ChatInteraction
{
    public Guid Id { get; init; }
    public Guid ChatSessionId { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string QuestionTextHash { get; init; } = string.Empty;
    public string? AnswerText { get; private set; }
    public AnswerState AnswerState { get; private set; }
    public Guid? RetrievalQueryId { get; private set; }
    public int RetrievalCandidateCount { get; private set; }
    public long? RetrievalLatencyMs { get; private set; }
    public long? GenerationLatencyMs { get; private set; }
    public long? TotalLatencyMs { get; private set; }
    public string? AiProvider { get; private set; }
    public string? AiModel { get; private set; }
    public int? TokenUsageInput { get; private set; }
    public int? TokenUsageOutput { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public string? ProviderFailureCode { get; private set; }
    public string? CorrelationId { get; init; }
    public string? PromptVersion { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static ChatInteraction Create(
        Guid sessionId,
        Guid orgId,
        Guid userId,
        string questionText,
        string questionHash,
        string? correlationId)
    {
        var now = DateTimeOffset.UtcNow;
        return new ChatInteraction
        {
            Id = Guid.NewGuid(),
            ChatSessionId = sessionId,
            OrganizationId = orgId,
            UserId = userId,
            QuestionText = questionText,
            QuestionTextHash = questionHash,
            AnswerState = AnswerState.Grounded,
            CorrelationId = correlationId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void RecordGroundedOutcome(
        string answerText,
        Guid? retrievalQueryId,
        int candidateCount,
        long? retrievalMs,
        long? generationMs,
        long? totalMs,
        int? inputTokens,
        int? outputTokens,
        decimal? cost,
        string? provider,
        string? model,
        string? promptVersion = null)
    {
        AnswerState = AnswerState.Grounded;
        AnswerText = answerText;
        RetrievalQueryId = retrievalQueryId;
        RetrievalCandidateCount = candidateCount;
        RetrievalLatencyMs = retrievalMs;
        GenerationLatencyMs = generationMs;
        TotalLatencyMs = totalMs;
        TokenUsageInput = inputTokens;
        TokenUsageOutput = outputTokens;
        EstimatedCost = cost;
        AiProvider = provider;
        AiModel = model;
        PromptVersion = promptVersion;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordInsufficientContextOutcome(
        Guid? retrievalQueryId,
        int candidateCount,
        long? retrievalMs,
        long? totalMs)
    {
        AnswerState = AnswerState.InsufficientContext;
        RetrievalQueryId = retrievalQueryId;
        RetrievalCandidateCount = candidateCount;
        RetrievalLatencyMs = retrievalMs;
        TotalLatencyMs = totalMs;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordProviderFailedOutcome(
        string? safeFailureCode,
        Guid? retrievalQueryId,
        int candidateCount,
        long? retrievalMs,
        long? generationMs,
        long? totalMs,
        string? aiProvider = null,
        string? aiModel = null)
    {
        AnswerState = AnswerState.ProviderFailed;
        ProviderFailureCode = safeFailureCode;
        RetrievalQueryId = retrievalQueryId;
        RetrievalCandidateCount = candidateCount;
        RetrievalLatencyMs = retrievalMs;
        GenerationLatencyMs = generationMs;
        TotalLatencyMs = totalMs;
        AiProvider = aiProvider;
        AiModel = aiModel;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

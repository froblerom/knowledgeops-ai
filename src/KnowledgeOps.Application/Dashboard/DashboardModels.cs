namespace KnowledgeOps.Application.Dashboard;

/// <summary>
/// Overview aggregate result — combines counts from chat, documents, and feedback for the period.
/// </summary>
public sealed record DashboardOverviewResult(
    int QuestionsAsked,
    int ActiveUsers,
    int DocumentsUploaded,
    int DocumentsProcessed,
    int DocumentsFailed,
    long? AverageResponseLatencyMs,
    int InsufficientContextCount,
    int ProviderFailureCount,
    int UsefulFeedbackCount,
    int NotUsefulFeedbackCount,
    bool EstimatedCostAvailable,
    decimal? EstimatedCostTotal);

/// <summary>
/// Document aggregate result — counts by processing status for the org.
/// </summary>
public sealed record DashboardDocumentsResult(
    int Uploaded,
    int Processing,
    int Processed,
    int Failed,
    int RetrievalDisabled);

/// <summary>
/// Chat aggregate result — latency, token, cost, and outcome counts for the period.
/// </summary>
public sealed record DashboardChatResult(
    int QuestionsAsked,
    int ActiveUsers,
    long? AverageResponseLatencyMs,
    long? RetrievalLatencyMs,
    long? GenerationLatencyMs,
    long? TotalRagLatencyMs,
    int InsufficientContextCount,
    int ProviderFailureCount,
    long? TokenInputTotal,
    long? TokenOutputTotal,
    bool EstimatedCostAvailable,
    decimal? EstimatedCostTotal);

/// <summary>
/// Feedback aggregate result — useful and not-useful counts for the period.
/// </summary>
public sealed record DashboardFeedbackResult(
    int Useful,
    int NotUseful);

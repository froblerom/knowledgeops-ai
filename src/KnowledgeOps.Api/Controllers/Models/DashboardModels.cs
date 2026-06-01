namespace KnowledgeOps.Api.Controllers.Models;

// ── Shared types ──────────────────────────────────────────────────────────────

public sealed record DashboardPeriodResponse(
    DateTimeOffset From,
    DateTimeOffset To);

public sealed record DashboardCostResponse(
    bool Available,
    decimal? EstimatedTotal);

public sealed record DashboardTokensResponse(
    long? Input,
    long? Output,
    long? Total);

// ── Overview response ─────────────────────────────────────────────────────────

public sealed record DashboardOverviewResponse(
    DashboardPeriodResponse Period,
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
    DashboardCostResponse Cost);

// ── Documents response ────────────────────────────────────────────────────────

public sealed record DashboardDocumentsResponse(
    DashboardPeriodResponse Period,
    int Uploaded,
    int Processing,
    int Processed,
    int Failed,
    int RetrievalDisabled);

// ── Chat response ─────────────────────────────────────────────────────────────

public sealed record DashboardChatResponse(
    DashboardPeriodResponse Period,
    int QuestionsAsked,
    int ActiveUsers,
    long? AverageResponseLatencyMs,
    long? RetrievalLatencyMs,
    long? GenerationLatencyMs,
    long? TotalRagLatencyMs,
    int InsufficientContextCount,
    int ProviderFailureCount,
    DashboardTokensResponse Tokens,
    DashboardCostResponse Cost);

// ── Feedback response ─────────────────────────────────────────────────────────

public sealed record DashboardFeedbackResponse(
    DashboardPeriodResponse Period,
    int Useful,
    int NotUseful,
    int Total);

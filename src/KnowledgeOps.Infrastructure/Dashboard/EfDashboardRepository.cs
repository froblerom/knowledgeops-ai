using KnowledgeOps.Application.Dashboard;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Dashboard;

/// <summary>
/// EF Core implementation of IDashboardRepository.
/// CRITICAL: All queries filter by organizationId FIRST before any aggregation.
/// Use typed enum casts — never magic integer literals for AnswerState.
/// Cost/latency nullability: null means unavailable — never coerce to 0.
/// </summary>
internal sealed class EfDashboardRepository(KnowledgeOpsDbContext db) : IDashboardRepository
{
    public async Task<DashboardOverviewResult> GetOverviewAsync(
        Guid organizationId,
        DashboardDateRange range,
        CancellationToken ct = default)
    {
        // Chat interactions scoped to org and period
        var chatQuery = db.ChatInteractions
            .Where(i => i.OrganizationId == organizationId
                        && i.CreatedAt >= range.From
                        && i.CreatedAt <= range.To);

        var questionsAsked = await chatQuery.CountAsync(ct);

        // Active users = COUNT(DISTINCT user_id) within period (Decision D5)
        var activeUsers = await chatQuery
            .Select(i => i.UserId)
            .Distinct()
            .CountAsync(ct);

        // Average total latency — only non-null rows; null when no data
        var avgLatency = await chatQuery
            .Where(i => i.TotalLatencyMs != null)
            .Select(i => (double?)i.TotalLatencyMs)
            .AverageAsync(ct);

        var avgLatencyMs = avgLatency.HasValue ? (long?)Convert.ToInt64(avgLatency.Value) : null;

        // InsufficientContext and ProviderFailed counts — typed enum casts, never magic literals
        var insufficientContextCount = await chatQuery
            .CountAsync(i => (int)i.AnswerState == (int)AnswerState.InsufficientContext, ct);

        var providerFailureCount = await chatQuery
            .CountAsync(i => (int)i.AnswerState == (int)AnswerState.ProviderFailed, ct);

        // Cost — available only when at least one row has a non-null estimated_cost
        var hasAnyCost = await chatQuery.AnyAsync(i => i.EstimatedCost != null, ct);
        var estimatedCostTotal = hasAnyCost
            ? await chatQuery.Where(i => i.EstimatedCost != null).SumAsync(i => i.EstimatedCost, ct)
            : null;

        // Documents — use created_at for period filter, exclude soft-deleted
        var docQuery = db.Documents
            .Where(d => d.OrganizationId == organizationId
                        && d.DeletedAt == null
                        && d.CreatedAt >= range.From
                        && d.CreatedAt <= range.To);

        var documentsUploaded = await docQuery.CountAsync(ct);
        var documentsProcessed = await docQuery
            .CountAsync(d => d.ProcessingStatus == DocumentProcessingStatus.Processed, ct);
        var documentsFailed = await docQuery
            .CountAsync(d => d.ProcessingStatus == DocumentProcessingStatus.Failed, ct);

        // Feedback scoped to org and period
        var feedbackQuery = db.AnswerFeedback
            .Where(f => f.OrganizationId == organizationId
                        && f.CreatedAt >= range.From
                        && f.CreatedAt <= range.To);

        var usefulCount = await feedbackQuery
            .CountAsync(f => f.Rating == AnswerFeedbackRating.Useful, ct);

        var notUsefulCount = await feedbackQuery
            .CountAsync(f => f.Rating == AnswerFeedbackRating.NotUseful, ct);

        return new DashboardOverviewResult(
            questionsAsked,
            activeUsers,
            documentsUploaded,
            documentsProcessed,
            documentsFailed,
            avgLatencyMs,
            insufficientContextCount,
            providerFailureCount,
            usefulCount,
            notUsefulCount,
            hasAnyCost,
            estimatedCostTotal);
    }

    public async Task<DashboardDocumentsResult> GetDocumentsAsync(
        Guid organizationId,
        DashboardDateRange range,
        CancellationToken ct = default)
    {
        // All non-deleted documents for the org (not period-filtered — shows current state)
        var docQuery = db.Documents
            .Where(d => d.OrganizationId == organizationId && d.DeletedAt == null);

        var uploaded = await docQuery
            .CountAsync(d => d.ProcessingStatus == DocumentProcessingStatus.Uploaded, ct);

        var processing = await docQuery
            .CountAsync(d => d.ProcessingStatus == DocumentProcessingStatus.Processing, ct);

        var processed = await docQuery
            .CountAsync(d => d.ProcessingStatus == DocumentProcessingStatus.Processed, ct);

        var failed = await docQuery
            .CountAsync(d => d.ProcessingStatus == DocumentProcessingStatus.Failed, ct);

        // retrievalDisabled: processed but retrieval is disabled
        var retrievalDisabled = await docQuery
            .CountAsync(d => !d.IsRetrievalEnabled, ct);

        return new DashboardDocumentsResult(
            uploaded,
            processing,
            processed,
            failed,
            retrievalDisabled);
    }

    public async Task<DashboardChatResult> GetChatAsync(
        Guid organizationId,
        DashboardDateRange range,
        CancellationToken ct = default)
    {
        var chatQuery = db.ChatInteractions
            .Where(i => i.OrganizationId == organizationId
                        && i.CreatedAt >= range.From
                        && i.CreatedAt <= range.To);

        var questionsAsked = await chatQuery.CountAsync(ct);

        // Active users = COUNT(DISTINCT user_id) (Decision D5)
        var activeUsers = await chatQuery
            .Select(i => i.UserId)
            .Distinct()
            .CountAsync(ct);

        // Latency averages — only from non-null rows; null when no data
        var avgTotal = await chatQuery
            .Where(i => i.TotalLatencyMs != null)
            .Select(i => (double?)i.TotalLatencyMs)
            .AverageAsync(ct);
        var avgTotalMs = avgTotal.HasValue ? (long?)Convert.ToInt64(avgTotal.Value) : null;

        var avgRetrieval = await chatQuery
            .Where(i => i.RetrievalLatencyMs != null)
            .Select(i => (double?)i.RetrievalLatencyMs)
            .AverageAsync(ct);
        var avgRetrievalMs = avgRetrieval.HasValue ? (long?)Convert.ToInt64(avgRetrieval.Value) : null;

        var avgGeneration = await chatQuery
            .Where(i => i.GenerationLatencyMs != null)
            .Select(i => (double?)i.GenerationLatencyMs)
            .AverageAsync(ct);
        var avgGenerationMs = avgGeneration.HasValue ? (long?)Convert.ToInt64(avgGeneration.Value) : null;

        // TotalRagLatencyMs = average of total latency (same as AverageResponseLatencyMs for overview)
        var totalRagMs = avgTotalMs;

        // Answer state counts — typed enum casts
        var insufficientContextCount = await chatQuery
            .CountAsync(i => (int)i.AnswerState == (int)AnswerState.InsufficientContext, ct);

        var providerFailureCount = await chatQuery
            .CountAsync(i => (int)i.AnswerState == (int)AnswerState.ProviderFailed, ct);

        // Token sums — only from non-null rows; null when no data
        var hasAnyInput = await chatQuery.AnyAsync(i => i.TokenUsageInput != null, ct);
        var tokenInputTotal = hasAnyInput
            ? (long?)await chatQuery.Where(i => i.TokenUsageInput != null).SumAsync(i => (long)i.TokenUsageInput!, ct)
            : null;

        var hasAnyOutput = await chatQuery.AnyAsync(i => i.TokenUsageOutput != null, ct);
        var tokenOutputTotal = hasAnyOutput
            ? (long?)await chatQuery.Where(i => i.TokenUsageOutput != null).SumAsync(i => (long)i.TokenUsageOutput!, ct)
            : null;

        // Cost — available only when at least one row has cost
        var hasAnyCost = await chatQuery.AnyAsync(i => i.EstimatedCost != null, ct);
        var estimatedCostTotal = hasAnyCost
            ? await chatQuery.Where(i => i.EstimatedCost != null).SumAsync(i => i.EstimatedCost, ct)
            : null;

        return new DashboardChatResult(
            questionsAsked,
            activeUsers,
            avgTotalMs,
            avgRetrievalMs,
            avgGenerationMs,
            totalRagMs,
            insufficientContextCount,
            providerFailureCount,
            tokenInputTotal,
            tokenOutputTotal,
            hasAnyCost,
            estimatedCostTotal);
    }

    public async Task<DashboardFeedbackResult> GetFeedbackAsync(
        Guid organizationId,
        DashboardDateRange range,
        CancellationToken ct = default)
    {
        var feedbackQuery = db.AnswerFeedback
            .Where(f => f.OrganizationId == organizationId
                        && f.CreatedAt >= range.From
                        && f.CreatedAt <= range.To);

        var useful = await feedbackQuery
            .CountAsync(f => f.Rating == AnswerFeedbackRating.Useful, ct);

        var notUseful = await feedbackQuery
            .CountAsync(f => f.Rating == AnswerFeedbackRating.NotUseful, ct);

        return new DashboardFeedbackResult(useful, notUseful);
    }
}

using KnowledgeOps.Api.Authorization;
using KnowledgeOps.Api.Controllers.Models;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    [HttpGet("overview")]
    [RequirePermission(KnowledgeOpsPermissions.Dashboard.ViewOverview)]
    public async Task<ActionResult<DashboardOverviewResponse>> GetOverview(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var range = DashboardDateRange.Create(from, to);
        var result = await service.GetOverviewAsync(range, ct);

        return Ok(new DashboardOverviewResponse(
            Period: new DashboardPeriodResponse(range.From, range.To),
            QuestionsAsked: result.QuestionsAsked,
            ActiveUsers: result.ActiveUsers,
            DocumentsUploaded: result.DocumentsUploaded,
            DocumentsProcessed: result.DocumentsProcessed,
            DocumentsFailed: result.DocumentsFailed,
            AverageResponseLatencyMs: result.AverageResponseLatencyMs,
            InsufficientContextCount: result.InsufficientContextCount,
            ProviderFailureCount: result.ProviderFailureCount,
            UsefulFeedbackCount: result.UsefulFeedbackCount,
            NotUsefulFeedbackCount: result.NotUsefulFeedbackCount,
            Cost: new DashboardCostResponse(result.EstimatedCostAvailable, result.EstimatedCostTotal)));
    }

    [HttpGet("documents")]
    [RequirePermission(KnowledgeOpsPermissions.Dashboard.ViewDocuments)]
    public async Task<ActionResult<DashboardDocumentsResponse>> GetDocuments(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var range = DashboardDateRange.Create(from, to);
        var result = await service.GetDocumentsAsync(range, ct);

        return Ok(new DashboardDocumentsResponse(
            Period: new DashboardPeriodResponse(range.From, range.To),
            Uploaded: result.Uploaded,
            Processing: result.Processing,
            Processed: result.Processed,
            Failed: result.Failed,
            RetrievalDisabled: result.RetrievalDisabled));
    }

    [HttpGet("chat")]
    [RequirePermission(KnowledgeOpsPermissions.Dashboard.ViewChat)]
    public async Task<ActionResult<DashboardChatResponse>> GetChat(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var range = DashboardDateRange.Create(from, to);
        var result = await service.GetChatAsync(range, ct);

        var tokenTotal = result.TokenInputTotal.HasValue || result.TokenOutputTotal.HasValue
            ? (long?)((result.TokenInputTotal ?? 0) + (result.TokenOutputTotal ?? 0))
            : null;

        return Ok(new DashboardChatResponse(
            Period: new DashboardPeriodResponse(range.From, range.To),
            QuestionsAsked: result.QuestionsAsked,
            ActiveUsers: result.ActiveUsers,
            AverageResponseLatencyMs: result.AverageResponseLatencyMs,
            RetrievalLatencyMs: result.RetrievalLatencyMs,
            GenerationLatencyMs: result.GenerationLatencyMs,
            TotalRagLatencyMs: result.TotalRagLatencyMs,
            InsufficientContextCount: result.InsufficientContextCount,
            ProviderFailureCount: result.ProviderFailureCount,
            Tokens: new DashboardTokensResponse(
                result.TokenInputTotal,
                result.TokenOutputTotal,
                tokenTotal),
            Cost: new DashboardCostResponse(result.EstimatedCostAvailable, result.EstimatedCostTotal)));
    }

    [HttpGet("feedback")]
    [RequirePermission(KnowledgeOpsPermissions.Dashboard.ViewFeedback)]
    public async Task<ActionResult<DashboardFeedbackResponse>> GetFeedback(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var range = DashboardDateRange.Create(from, to);
        var result = await service.GetFeedbackAsync(range, ct);

        return Ok(new DashboardFeedbackResponse(
            Period: new DashboardPeriodResponse(range.From, range.To),
            Useful: result.Useful,
            NotUseful: result.NotUseful,
            Total: result.Useful + result.NotUseful));
    }
}

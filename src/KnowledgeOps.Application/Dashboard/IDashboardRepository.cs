namespace KnowledgeOps.Application.Dashboard;

/// <summary>
/// Repository interface for aggregated dashboard queries.
/// Owned by Application — implemented by Infrastructure (EfDashboardRepository).
/// All implementations MUST filter by organizationId first before aggregation.
/// </summary>
public interface IDashboardRepository
{
    Task<DashboardOverviewResult> GetOverviewAsync(
        Guid organizationId,
        DashboardDateRange range,
        CancellationToken ct = default);

    Task<DashboardDocumentsResult> GetDocumentsAsync(
        Guid organizationId,
        DashboardDateRange range,
        CancellationToken ct = default);

    Task<DashboardChatResult> GetChatAsync(
        Guid organizationId,
        DashboardDateRange range,
        CancellationToken ct = default);

    Task<DashboardFeedbackResult> GetFeedbackAsync(
        Guid organizationId,
        DashboardDateRange range,
        CancellationToken ct = default);
}

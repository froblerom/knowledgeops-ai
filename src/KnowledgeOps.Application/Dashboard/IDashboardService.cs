namespace KnowledgeOps.Application.Dashboard;

/// <summary>
/// Application service for dashboard metric aggregations.
/// Organization scope is derived from IUserAccessStateReader — never from caller input.
/// </summary>
public interface IDashboardService
{
    Task<DashboardOverviewResult> GetOverviewAsync(
        DashboardDateRange range,
        CancellationToken ct = default);

    Task<DashboardDocumentsResult> GetDocumentsAsync(
        DashboardDateRange range,
        CancellationToken ct = default);

    Task<DashboardChatResult> GetChatAsync(
        DashboardDateRange range,
        CancellationToken ct = default);

    Task<DashboardFeedbackResult> GetFeedbackAsync(
        DashboardDateRange range,
        CancellationToken ct = default);
}

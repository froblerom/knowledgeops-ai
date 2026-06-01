using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Errors;

namespace KnowledgeOps.Application.Dashboard;

internal sealed class DashboardService(
    ICurrentUser currentUser,
    IUserAccessStateReader accessStateReader,
    IPermissionService permissionService,
    IDashboardRepository repository) : IDashboardService
{
    public async Task<DashboardOverviewResult> GetOverviewAsync(
        DashboardDateRange range,
        CancellationToken ct = default)
    {
        var state = await RequireActiveStateAsync(ct);
        RequirePermission(state, KnowledgeOpsPermissions.Dashboard.ViewOverview);
        return await repository.GetOverviewAsync(state.OrganizationId, range, ct);
    }

    public async Task<DashboardDocumentsResult> GetDocumentsAsync(
        DashboardDateRange range,
        CancellationToken ct = default)
    {
        var state = await RequireActiveStateAsync(ct);
        RequirePermission(state, KnowledgeOpsPermissions.Dashboard.ViewDocuments);
        return await repository.GetDocumentsAsync(state.OrganizationId, range, ct);
    }

    public async Task<DashboardChatResult> GetChatAsync(
        DashboardDateRange range,
        CancellationToken ct = default)
    {
        var state = await RequireActiveStateAsync(ct);
        RequirePermission(state, KnowledgeOpsPermissions.Dashboard.ViewChat);
        return await repository.GetChatAsync(state.OrganizationId, range, ct);
    }

    public async Task<DashboardFeedbackResult> GetFeedbackAsync(
        DashboardDateRange range,
        CancellationToken ct = default)
    {
        var state = await RequireActiveStateAsync(ct);
        RequirePermission(state, KnowledgeOpsPermissions.Dashboard.ViewFeedback);
        return await repository.GetFeedbackAsync(state.OrganizationId, range, ct);
    }

    private async Task<UserAccessState> RequireActiveStateAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            throw new ApplicationUnauthenticatedException();

        var state = await accessStateReader.FindActiveByIdAsync(currentUser.UserId, ct);
        if (state is null)
            throw new ApplicationUnauthenticatedException();

        if (state.OrganizationId == Guid.Empty)
            throw new ApplicationForbiddenException();

        return state;
    }

    private void RequirePermission(UserAccessState state, string permission)
    {
        if (!permissionService.HasPermission(state, permission))
            throw new ApplicationForbiddenException();
    }
}

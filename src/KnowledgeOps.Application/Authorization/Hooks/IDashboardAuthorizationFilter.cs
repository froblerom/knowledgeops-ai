namespace KnowledgeOps.Application.Authorization.Hooks;

// Future contract: applied before dashboard metric queries execute.
// All aggregations must be scoped to the user's organization; no cross-organization aggregation in MVP.
// Do NOT implement dashboard metrics, chart data, or export here.
// Sprint 23+ implements dashboard workflows using this contract.
public interface IDashboardAuthorizationFilter
{
    Guid GetRequiredOrganizationScope(Guid currentUserOrganizationId);
}

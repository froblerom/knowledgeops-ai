using KnowledgeOps.Api.Authorization;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Api.Tests.Authorization;

// Test-only controller registered in AuthorizationApiTestFactory.
// Not a production endpoint. Exists solely to validate authorization policies
// without introducing document, chat, dashboard, or admin business endpoints.
[ApiController]
[Route("api/test/authorization")]
public sealed class TestPermissionController : ControllerBase
{
    // Requires Documents.Upload — KnowledgeAdmin and Admin only.
    [HttpGet("documents-upload")]
    [RequirePermission(KnowledgeOpsPermissions.Documents.Upload)]
    public IActionResult RequireDocumentsUpload() =>
        Ok(new { granted = true, permission = "Documents.Upload" });

    // Requires Users.View — Admin only.
    [HttpGet("users-view")]
    [RequirePermission(KnowledgeOpsPermissions.Users.View)]
    public IActionResult RequireUsersView() =>
        Ok(new { granted = true, permission = "Users.View" });

    // Requires Dashboard.ViewOverview — KnowledgeAdmin, Manager, Admin.
    [HttpGet("dashboard-overview")]
    [RequirePermission(KnowledgeOpsPermissions.Dashboard.ViewOverview)]
    public IActionResult RequireDashboardViewOverview() =>
        Ok(new { granted = true, permission = "Dashboard.ViewOverview" });

    // Requires Chat.AskQuestion — all five MVP roles.
    [HttpGet("chat-ask")]
    [RequirePermission(KnowledgeOpsPermissions.Chat.AskQuestion)]
    public IActionResult RequireChatAskQuestion() =>
        Ok(new { granted = true, permission = "Chat.AskQuestion" });

    // Organization-scope check: returns 404 when cross-organization, 200 when in scope.
    // Uses IOrganizationScopeService. No business table or document lookup needed.
    // Cross-organization resource lookup by ID must return 404 to hide resource existence.
    [HttpGet("scope-check")]
    [Authorize]
    public IActionResult ScopeCheck(
        [FromQuery] Guid targetOrgId,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IOrganizationScopeService scopeService)
    {
        var result = scopeService.CheckScope(currentUser.OrganizationId, targetOrgId);
        if (!result.IsAllowed)
            return NotFound(); // 404 — cross-org access must not reveal resource existence.

        return Ok(new { inScope = true });
    }
}

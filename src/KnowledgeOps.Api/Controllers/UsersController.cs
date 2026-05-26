using KnowledgeOps.Api.Authorization;
using KnowledgeOps.Api.Controllers.Models;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Users;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(UserManagementService service, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(KnowledgeOpsPermissions.Users.View)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(CancellationToken ct)
    {
        var users = await service.ListAsync(Actor(), ct);
        return Ok(users.Select(ToResponse).ToArray());
    }

    [HttpPost]
    [RequirePermission(KnowledgeOpsPermissions.Users.Create)]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        var user = await service.CreateAsync(
            Actor(),
            new CreateManagedUserCommand(
                request.DisplayName,
                request.Email,
                request.OrganizationId,
                request.Status,
                request.Roles ?? [],
                request.InitialPassword),
            ct);

        return CreatedAtAction(nameof(Get), new { userId = user.UserId }, ToResponse(user));
    }

    [HttpGet("{userId:guid}")]
    [RequirePermission(KnowledgeOpsPermissions.Users.View)]
    public async Task<ActionResult<UserResponse>> Get(Guid userId, CancellationToken ct) =>
        Ok(ToResponse(await service.GetAsync(Actor(), userId, ct)));

    [HttpPut("{userId:guid}")]
    [RequirePermission(KnowledgeOpsPermissions.Users.Update)]
    public async Task<ActionResult<UserResponse>> Update(
        Guid userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct) =>
        Ok(ToResponse(await service.UpdateAsync(
            Actor(),
            userId,
            new UpdateManagedUserCommand(
                request.DisplayName,
                request.Email,
                request.OrganizationId,
                request.Status),
            ct)));

    [HttpPost("{userId:guid}/roles")]
    [RequirePermission(KnowledgeOpsPermissions.Users.AssignRole)]
    public async Task<ActionResult<UserResponse>> AddRole(
        Guid userId,
        [FromBody] AssignRoleRequest request,
        CancellationToken ct) =>
        Ok(ToResponse(await service.AddRoleAsync(Actor(), userId, request.RoleName, ct)));

    [HttpDelete("{userId:guid}/roles/{roleName}")]
    [RequirePermission(KnowledgeOpsPermissions.Users.RemoveRole)]
    public async Task<ActionResult<UserResponse>> RemoveRole(
        Guid userId,
        string roleName,
        CancellationToken ct) =>
        Ok(ToResponse(await service.RemoveRoleAsync(Actor(), userId, roleName, ct)));

    private UserManagementActor Actor() => new(currentUser.UserId, currentUser.OrganizationId);

    private static UserResponse ToResponse(ManagedUser user) =>
        new()
        {
            UserId = user.UserId,
            DisplayName = user.DisplayName,
            Email = user.Email,
            OrganizationId = user.OrganizationId,
            Status = user.Status.ToString(),
            Roles = user.Roles,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt
        };
}

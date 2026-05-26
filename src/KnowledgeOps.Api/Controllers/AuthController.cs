using KnowledgeOps.Api.Controllers.Models;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Auth.Commands;
using KnowledgeOps.Application.Auth.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginCommandHandler _loginCommandHandler;
    private readonly GetCurrentUserQueryHandler _getCurrentUserQueryHandler;
    private readonly ICurrentUser _currentUser;

    public AuthController(
        LoginCommandHandler loginCommandHandler,
        GetCurrentUserQueryHandler getCurrentUserQueryHandler,
        ICurrentUser currentUser)
    {
        _loginCommandHandler = loginCommandHandler;
        _getCurrentUserQueryHandler = getCurrentUserQueryHandler;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _loginCommandHandler.HandleAsync(
            new LoginCommand(request.Email, request.Password), ct);

        if (result is null)
            return Unauthorized(new { message = "Invalid credentials." });

        return Ok(new LoginResponse
        {
            AccessToken = result.AccessToken,
            ExpiresAt = result.ExpiresAt,
            User = new CurrentUserResponse
            {
                UserId = result.UserId,
                Email = result.Email,
                DisplayName = result.DisplayName,
                OrganizationId = result.OrganizationId,
                Roles = result.Roles,
                Status = "Active"
            }
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await _getCurrentUserQueryHandler.HandleAsync(
            new GetCurrentUserQuery(_currentUser.UserId), ct);

        if (result is null)
            return Unauthorized(new { message = "Invalid credentials." });

        return Ok(new CurrentUserResponse
        {
            UserId = result.UserId,
            Email = result.Email,
            DisplayName = result.DisplayName,
            OrganizationId = result.OrganizationId,
            Roles = result.Roles,
            Status = result.Status
        });
    }
}

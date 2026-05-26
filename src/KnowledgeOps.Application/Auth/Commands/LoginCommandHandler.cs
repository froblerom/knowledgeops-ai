using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Domain.Users;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Auth.Commands;

public sealed class LoginCommandHandler
{
    private readonly IUserAuthRepository _userAuthRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserAuthRepository userAuthRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _userAuthRepository = userAuthRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResult?> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var user = await _userAuthRepository.FindByEmailAsync(command.Email, ct);

        if (user is null
            || user.Status != UserStatus.Active
            || string.IsNullOrEmpty(user.PasswordHash)
            || !_passwordHasher.VerifyPassword(user.PasswordHash, command.Password))
        {
            _logger.LogWarning(
                "Authentication attempt failed. EventType=LoginFailed ReasonCode=InvalidCredentials");
            return null;
        }

        var tokenResult = _tokenService.IssueToken(
            user.UserId,
            user.OrganizationId,
            user.Email,
            user.DisplayName,
            user.Roles);

        await _userAuthRepository.UpdateLastLoginAtAsync(user.UserId, DateTimeOffset.UtcNow, ct);

        _logger.LogInformation(
            "Authentication succeeded. EventType=LoginSucceeded UserId={UserId} OrganizationId={OrganizationId}",
            user.UserId,
            user.OrganizationId);

        return new LoginResult(
            tokenResult.AccessToken,
            tokenResult.ExpiresAt,
            user.UserId,
            user.Email,
            user.DisplayName,
            user.OrganizationId,
            user.Roles);
    }
}

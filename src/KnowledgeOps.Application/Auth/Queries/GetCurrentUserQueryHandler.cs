using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Domain.Users;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Auth.Queries;

public sealed class GetCurrentUserQueryHandler
{
    private readonly IUserAuthRepository _userAuthRepository;
    private readonly ILogger<GetCurrentUserQueryHandler> _logger;

    public GetCurrentUserQueryHandler(
        IUserAuthRepository userAuthRepository,
        ILogger<GetCurrentUserQueryHandler> logger)
    {
        _userAuthRepository = userAuthRepository;
        _logger = logger;
    }

    public async Task<CurrentUserResult?> HandleAsync(GetCurrentUserQuery query, CancellationToken ct = default)
    {
        var user = await _userAuthRepository.FindByIdAsync(query.UserId, ct);

        if (user is null)
        {
            _logger.LogWarning(
                "Current-user lookup failed. EventType=CurrentUserNotFound UserId={UserId}",
                query.UserId);
            return null;
        }

        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning(
                "Current-user access denied. EventType=CurrentUserDenied UserId={UserId} ReasonCode=AccountNotActive",
                query.UserId);
            return null;
        }

        return new CurrentUserResult(
            user.UserId,
            user.Email,
            user.DisplayName,
            user.OrganizationId,
            user.Roles,
            user.Status.ToString());
    }
}

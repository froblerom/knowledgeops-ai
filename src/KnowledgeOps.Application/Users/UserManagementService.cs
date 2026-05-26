using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Users;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Users;

public sealed class UserManagementService(
    IUserManagementRepository repository,
    IUserAccessStateReader accessStateReader,
    IPermissionService permissionService,
    IPasswordHasher passwordHasher,
    IAuditEventWriter auditEventWriter,
    ICorrelationContext correlationContext,
    ILogger<UserManagementService> logger)
{
    public Task<IReadOnlyList<ManagedUser>> ListAsync(
        UserManagementActor actor,
        CancellationToken ct = default) =>
        repository.ListAsync(actor.OrganizationId, ct);

    public async Task<ManagedUser> GetAsync(
        UserManagementActor actor,
        Guid userId,
        CancellationToken ct = default) =>
        await repository.FindAsync(userId, actor.OrganizationId, ct)
        ?? throw new ApplicationNotFoundException();

    public async Task<ManagedUser> CreateAsync(
        UserManagementActor actor,
        CreateManagedUserCommand command,
        CancellationToken ct = default)
    {
        var organizationId = command.OrganizationId ?? actor.OrganizationId;
        if (organizationId != actor.OrganizationId)
            throw new ApplicationNotFoundException();

        var displayName = ValidateDisplayName(command.DisplayName);
        var email = ValidateEmail(command.Email);
        var status = ValidateStatus(command.Status);
        var roles = command.Roles.Select(ValidateRole).Distinct().ToArray();
        if (string.IsNullOrWhiteSpace(command.InitialPassword))
            ThrowValidation("initialPassword", "An initial password is required.");

        var passwordHash = passwordHasher.HashPassword(command.InitialPassword);
        var user = await repository.CreateAsync(
            new NewManagedUser(
                Guid.NewGuid(),
                organizationId,
                displayName,
                email,
                passwordHash,
                status,
                roles,
                actor.UserId,
                DateTimeOffset.UtcNow),
            ct);

        await AuditAsync(
            AuditEventTypes.UserCreated,
            "User created.",
            actor,
            user.UserId,
            ct);

        foreach (var role in roles)
        {
            await AuditAsync(
                AuditEventTypes.UserRoleAssigned,
                "User role assigned.",
                actor,
                user.UserId,
                ct);
        }

        return user;
    }

    public async Task<ManagedUser> UpdateAsync(
        UserManagementActor actor,
        Guid userId,
        UpdateManagedUserCommand command,
        CancellationToken ct = default)
    {
        var existing = await GetAsync(actor, userId, ct);
        if (command.OrganizationId.HasValue && command.OrganizationId.Value != existing.OrganizationId)
            throw new ApplicationNotFoundException();

        var status = ValidateStatus(command.Status);
        if (actor.UserId == userId && status != UserStatus.Active)
        {
            await DeniedAsync(actor, userId, ct);
            throw new ApplicationForbiddenException();
        }

        if (existing.Status != UserStatus.Disabled && status == UserStatus.Disabled)
        {
            var actorState = await accessStateReader.FindActiveByIdAsync(actor.UserId, ct);
            if (actorState is null
                || actorState.OrganizationId != actor.OrganizationId
                || !permissionService.HasPermission(actorState, KnowledgeOpsPermissions.Users.Disable))
            {
                await DeniedAsync(actor, userId, ct);
                throw new ApplicationForbiddenException();
            }
        }

        var result = await repository.UpdateAsync(
            userId,
            actor.OrganizationId,
            new ManagedUserUpdate(
                ValidateDisplayName(command.DisplayName),
                ValidateEmail(command.Email),
                status,
                DateTimeOffset.UtcNow),
            ct);

        var user = await ResolveWriteResultAsync(result, actor, userId, ct);
        await AuditAsync(AuditEventTypes.UserUpdated, "User updated.", actor, userId, ct);
        if (result.StatusChanged)
            await AuditAsync(AuditEventTypes.UserStatusChanged, "User status changed.", actor, userId, ct);

        return user;
    }

    public async Task<ManagedUser> AddRoleAsync(
        UserManagementActor actor,
        Guid userId,
        string roleName,
        CancellationToken ct = default)
    {
        var result = await repository.AddRoleAsync(
            userId,
            actor.OrganizationId,
            ValidateRole(roleName),
            actor.UserId,
            DateTimeOffset.UtcNow,
            ct);
        var user = await ResolveWriteResultAsync(result, actor, userId, ct);
        if (result.Outcome == UserWriteOutcome.Applied)
            await AuditAsync(AuditEventTypes.UserRoleAssigned, "User role assigned.", actor, userId, ct);
        return user;
    }

    public async Task<ManagedUser> RemoveRoleAsync(
        UserManagementActor actor,
        Guid userId,
        string roleName,
        CancellationToken ct = default)
    {
        var role = ValidateRole(roleName);
        if (actor.UserId == userId && role == UserRoleName.Admin)
        {
            await DeniedAsync(actor, userId, ct);
            throw new ApplicationForbiddenException();
        }

        var result = await repository.RemoveRoleAsync(userId, actor.OrganizationId, role, ct);
        var user = await ResolveWriteResultAsync(result, actor, userId, ct);
        if (result.Outcome == UserWriteOutcome.Applied)
            await AuditAsync(AuditEventTypes.UserRoleRemoved, "User role removed.", actor, userId, ct);
        return user;
    }

    private async Task<ManagedUser> ResolveWriteResultAsync(
        UserWriteResult result,
        UserManagementActor actor,
        Guid userId,
        CancellationToken ct)
    {
        if (result.Outcome == UserWriteOutcome.NotFound)
            throw new ApplicationNotFoundException();
        if (result.Outcome == UserWriteOutcome.Conflict)
            throw new ApplicationConflictException();
        if (result.Outcome == UserWriteOutcome.FinalActiveAdmin)
        {
            await DeniedAsync(actor, userId, ct);
            throw new ApplicationForbiddenException();
        }

        return result.User ?? throw new InvalidOperationException("A successful user write returned no user.");
    }

    private static string ValidateDisplayName(string displayName)
    {
        var value = displayName.Trim();
        if (value.Length is 0 or > 200)
            ThrowValidation("displayName", "Display name must be between 1 and 200 characters.");
        return value;
    }

    private static string ValidateEmail(string email)
    {
        var value = EmailNormalizer.Normalize(email);
        if (value.Length is 0 or > 320 || !value.Contains('@', StringComparison.Ordinal))
            ThrowValidation("email", "A valid email is required.");
        return value;
    }

    private static UserStatus ValidateStatus(string status)
    {
        if (!Enum.TryParse<UserStatus>(status, false, out var result)
            || !Enum.IsDefined(result))
            ThrowValidation("status", "The status is invalid.");
        return result;
    }

    private static UserRoleName ValidateRole(string roleName)
    {
        if (!Enum.TryParse<UserRoleName>(roleName, false, out var result)
            || !Enum.IsDefined(result))
            ThrowValidation("roleName", "The role is invalid.");
        return result;
    }

    private static void ThrowValidation(string field, string message) =>
        throw new ApplicationValidationException([new ApplicationValidationItem(field, message)]);

    private Task DeniedAsync(UserManagementActor actor, Guid targetUserId, CancellationToken ct) =>
        AuditAsync(
            AuditEventTypes.UserManagementDenied,
            "User management action denied.",
            actor,
            targetUserId,
            ct,
            AuditSeverity.Warning);

    private async Task AuditAsync(
        string eventType,
        string message,
        UserManagementActor actor,
        Guid targetUserId,
        CancellationToken ct,
        AuditSeverity severity = AuditSeverity.Info)
    {
        try
        {
            await auditEventWriter.WriteAsync(
                new AuditEvent(
                    eventType,
                    message,
                    severity,
                    correlationContext.CorrelationId,
                    actor.OrganizationId,
                    actor.UserId,
                    "User",
                    targetUserId),
                ct);
        }
        catch
        {
            logger.LogWarning(
                "Audit write failed. EventType={EventType} CorrelationId={CorrelationId}",
                eventType,
                correlationContext.CorrelationId);
        }
    }
}

using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Application.Users;
using KnowledgeOps.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Users;

public sealed class UserManagementServiceTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid OtherOrgId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();
    private static readonly UserManagementActor Actor = new(ActorId, OrgId);

    [Fact]
    public async Task ListAsync_PassesOnlyActorOrganizationScope()
    {
        var repository = new FakeRepository();
        var service = BuildService(repository);

        await service.ListAsync(Actor);

        Assert.Equal(OrgId, repository.LastListOrganizationId);
    }

    [Fact]
    public async Task CreateAsync_NormalizesEmailHashesInitialPasswordAndAuditsSafely()
    {
        var repository = new FakeRepository();
        var audit = new RecordingAuditWriter();
        var hasher = new RecordingHasher();
        var service = BuildService(repository, audit: audit, hasher: hasher);

        var created = await service.CreateAsync(
            Actor,
            new CreateManagedUserCommand(
                " New User ",
                "  NEW.User@Example.COM ",
                null,
                "Active",
                ["Agent"],
                "Bootstrap-Secret"));

        Assert.Equal(OrgId, created.OrganizationId);
        Assert.Equal("new.user@example.com", created.Email);
        Assert.Equal("Bootstrap-Secret", hasher.LastPlaintext);
        Assert.Equal("HASHED", repository.LastCreated!.PasswordHash);
        Assert.DoesNotContain("Bootstrap-Secret", string.Join("|", audit.Events.Select(e => e.Message)));
        Assert.Contains(audit.Events, e => e.EventType == AuditEventTypes.UserCreated);
        Assert.Contains(audit.Events, e => e.EventType == AuditEventTypes.UserRoleAssigned);
    }

    [Fact]
    public async Task CreateAsync_CrossOrganizationTargetIsSafelyNotFound()
    {
        var service = BuildService(new FakeRepository());

        await Assert.ThrowsAsync<ApplicationNotFoundException>(() => service.CreateAsync(
            Actor,
            new CreateManagedUserCommand("User", "user@example.com", OtherOrgId, "Active", [], "pw")));
    }

    [Fact]
    public async Task CreateAsync_InvalidStatusOrRoleIsRejected()
    {
        var service = BuildService(new FakeRepository());

        await Assert.ThrowsAsync<ApplicationValidationException>(() => service.CreateAsync(
            Actor,
            new CreateManagedUserCommand("User", "user@example.com", null, "Deleted", [], "pw")));
        await Assert.ThrowsAsync<ApplicationValidationException>(() => service.CreateAsync(
            Actor,
            new CreateManagedUserCommand("User", "user@example.com", null, "Active", ["Owner"], "pw")));
    }

    [Fact]
    public async Task CreateAsync_DuplicateNormalizedEmailReturnsConflict()
    {
        var repository = new FakeRepository { ThrowConflictOnCreate = true };
        var service = BuildService(repository);

        await Assert.ThrowsAsync<ApplicationConflictException>(() => service.CreateAsync(
            Actor,
            new CreateManagedUserCommand("User", " EXISTING@Example.com ", null, "Active", [], "pw")));

        Assert.Equal("existing@example.com", repository.LastCreated!.Email);
    }

    [Fact]
    public async Task UpdateAsync_RejectsOrganizationMutationAndSelfDisable()
    {
        var audit = new RecordingAuditWriter();
        var service = BuildService(new FakeRepository(), audit: audit);

        await Assert.ThrowsAsync<ApplicationNotFoundException>(() => service.UpdateAsync(
            Actor,
            TargetId,
            new UpdateManagedUserCommand("User", "user@example.com", OtherOrgId, "Active")));
        await Assert.ThrowsAsync<ApplicationForbiddenException>(() => service.UpdateAsync(
            Actor,
            ActorId,
            new UpdateManagedUserCommand("Admin", "admin@example.com", OrgId, "Disabled")));

        Assert.Contains(audit.Events, e => e.EventType == AuditEventTypes.UserManagementDenied);
    }

    [Fact]
    public async Task UpdateAsync_DisablingRequiresCurrentDisablePermission()
    {
        var accessReader = new FakeAccessReader(new UserAccessState(ActorId, OrgId, ["Agent"]));
        var service = BuildService(new FakeRepository(), accessReader: accessReader);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(() => service.UpdateAsync(
            Actor,
            TargetId,
            new UpdateManagedUserCommand("User", "user@example.com", OrgId, "Disabled")));
    }

    [Fact]
    public async Task UpdateAsync_FinalActiveAdminOutcomeIsDeniedAndAudited()
    {
        var repository = new FakeRepository { UpdateOutcome = UserWriteOutcome.FinalActiveAdmin };
        var audit = new RecordingAuditWriter();
        var service = BuildService(repository, audit: audit);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(() => service.UpdateAsync(
            Actor,
            TargetId,
            new UpdateManagedUserCommand("User", "user@example.com", OrgId, "Pending")));

        Assert.Contains(audit.Events, e => e.EventType == AuditEventTypes.UserManagementDenied);
    }

    [Fact]
    public async Task AppliedStatusUpdateAndRoleRemovalEmitApprovedSafeAuditEvents()
    {
        var audit = new RecordingAuditWriter();
        var service = BuildService(new FakeRepository(), audit: audit);

        await service.UpdateAsync(
            Actor,
            TargetId,
            new UpdateManagedUserCommand("User", "user@example.com", OrgId, "Disabled"));
        await service.RemoveRoleAsync(Actor, TargetId, "Agent");

        Assert.Contains(audit.Events, e => e.EventType == AuditEventTypes.UserUpdated);
        Assert.Contains(audit.Events, e => e.EventType == AuditEventTypes.UserStatusChanged);
        Assert.Contains(audit.Events, e => e.EventType == AuditEventTypes.UserRoleRemoved);
        Assert.All(audit.Events, e => Assert.DoesNotContain("password", e.Message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RoleMutations_ProtectSelfAdminAndAuditOnlyAppliedAssignments()
    {
        var repository = new FakeRepository { AddOutcome = UserWriteOutcome.NoChange };
        var audit = new RecordingAuditWriter();
        var service = BuildService(repository, audit: audit);

        await service.AddRoleAsync(Actor, TargetId, "Agent");
        Assert.DoesNotContain(audit.Events, e => e.EventType == AuditEventTypes.UserRoleAssigned);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(
            () => service.RemoveRoleAsync(Actor, ActorId, "Admin"));
        Assert.Contains(audit.Events, e => e.EventType == AuditEventTypes.UserManagementDenied);
    }

    private static UserManagementService BuildService(
        FakeRepository repository,
        RecordingAuditWriter? audit = null,
        RecordingHasher? hasher = null,
        IUserAccessStateReader? accessReader = null) =>
        new(
            repository,
            accessReader ?? new FakeAccessReader(new UserAccessState(ActorId, OrgId, ["Admin"])),
            new PermissionService(),
            hasher ?? new RecordingHasher(),
            audit ?? new RecordingAuditWriter(),
            new StubCorrelationContext(),
            NullLogger<UserManagementService>.Instance);

    private sealed class FakeRepository : IUserManagementRepository
    {
        public Guid? LastListOrganizationId { get; private set; }
        public NewManagedUser? LastCreated { get; private set; }
        public bool ThrowConflictOnCreate { get; set; }
        public UserWriteOutcome UpdateOutcome { get; set; } = UserWriteOutcome.Applied;
        public UserWriteOutcome AddOutcome { get; set; } = UserWriteOutcome.Applied;

        public Task<IReadOnlyList<ManagedUser>> ListAsync(Guid organizationId, CancellationToken ct = default)
        {
            LastListOrganizationId = organizationId;
            return Task.FromResult<IReadOnlyList<ManagedUser>>([]);
        }

        public Task<ManagedUser?> FindAsync(Guid userId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<ManagedUser?>(MakeUser(userId, organizationId, UserStatus.Active));

        public Task<ManagedUser> CreateAsync(NewManagedUser user, CancellationToken ct = default)
        {
            LastCreated = user;
            if (ThrowConflictOnCreate)
                throw new ApplicationConflictException();
            return Task.FromResult(MakeUser(user.UserId, user.OrganizationId, user.Status, user.Email));
        }

        public Task<UserWriteResult> UpdateAsync(Guid userId, Guid organizationId, ManagedUserUpdate update, CancellationToken ct = default) =>
            Task.FromResult(new UserWriteResult(
                UpdateOutcome,
                UpdateOutcome is UserWriteOutcome.Applied or UserWriteOutcome.NoChange
                    ? MakeUser(userId, organizationId, update.Status, update.Email)
                    : null,
                true));

        public Task<UserWriteResult> AddRoleAsync(Guid userId, Guid organizationId, UserRoleName role, Guid assignedByUserId, DateTimeOffset assignedAt, CancellationToken ct = default) =>
            Task.FromResult(new UserWriteResult(AddOutcome, MakeUser(userId, organizationId, UserStatus.Active)));

        public Task<UserWriteResult> RemoveRoleAsync(Guid userId, Guid organizationId, UserRoleName role, CancellationToken ct = default) =>
            Task.FromResult(new UserWriteResult(UserWriteOutcome.Applied, MakeUser(userId, organizationId, UserStatus.Active)));

        private static ManagedUser MakeUser(
            Guid id,
            Guid organizationId,
            UserStatus status,
            string email = "user@example.com") =>
            new(id, "User", email, organizationId, status, ["Agent"], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);
    }

    private sealed class FakeAccessReader(UserAccessState? state) : IUserAccessStateReader
    {
        public Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(state);
    }

    private sealed class RecordingHasher : IPasswordHasher
    {
        public string? LastPlaintext { get; private set; }
        public string HashPassword(string password)
        {
            LastPlaintext = password;
            return "HASHED";
        }
        public bool VerifyPassword(string hashedPassword, string password) => false;
    }

    private sealed class RecordingAuditWriter : IAuditEventWriter
    {
        public List<AuditEvent> Events { get; } = [];
        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class StubCorrelationContext : ICorrelationContext
    {
        public string CorrelationId => "safe-correlation-id";
    }
}

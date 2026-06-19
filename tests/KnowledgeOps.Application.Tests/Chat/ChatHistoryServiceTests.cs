using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Application.Chat.Citations;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Chat;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Chat;

public sealed class ChatHistoryServiceTests
{
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherOrgId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SessionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid InteractionId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    // ── GetSessions ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSessions_OwnOnly_FiltersToCurrentUserAndOrg()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);
        harness.Sessions.Add(MakeSession(OwnerId, OrgId));
        harness.Sessions.Add(MakeSession(OtherId, OrgId));

        var result = await harness.Service.GetSessionsAsync(scopedReview: false);

        Assert.Single(result);
        Assert.Equal(OwnerId, harness.Sessions.First(s => s.Id == result[0].ChatSessionId).UserId);
    }

    [Fact]
    public async Task GetSessions_ScopedReview_AllowedForSupervisor()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Supervisor"]);
        harness.Sessions.Add(MakeSession(OwnerId, OrgId));
        harness.Sessions.Add(MakeSession(OtherId, OrgId));

        var result = await harness.Service.GetSessionsAsync(scopedReview: true);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetSessions_ScopedReview_DeniedForAgent()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);
        harness.Sessions.Add(MakeSession(OtherId, OrgId));

        var result = await harness.Service.GetSessionsAsync(scopedReview: true);

        Assert.Empty(result);
        Assert.Contains(harness.Audit.Events, e => e.EventType == AuditEventTypes.ChatHistoryDenied);
    }

    [Fact]
    public async Task GetSessions_ScopedReview_DeniedForKnowledgeAdmin()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["KnowledgeAdmin"]);
        harness.Sessions.Add(MakeSession(OtherId, OrgId));

        var result = await harness.Service.GetSessionsAsync(scopedReview: true);

        Assert.Empty(result);
        Assert.Contains(harness.Audit.Events, e => e.EventType == AuditEventTypes.ChatHistoryDenied);
    }

    [Fact]
    public async Task GetSessions_ScopedReview_AllowedForManager()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Manager"]);
        harness.Sessions.Add(MakeSession(OtherId, OrgId));

        var result = await harness.Service.GetSessionsAsync(scopedReview: true);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetSessions_ScopedReview_AllowedForAdmin()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Admin"]);
        harness.Sessions.Add(MakeSession(OtherId, OrgId));

        var result = await harness.Service.GetSessionsAsync(scopedReview: true);

        Assert.Single(result);
    }

    // ── CreateSession ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSession_CreatesActiveSession_WithCorrectOrgAndUser()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);

        var sessionId = await harness.Service.CreateSessionAsync("Test session");

        var stored = harness.Sessions.SingleOrDefault(s => s.Id == sessionId);
        Assert.NotNull(stored);
        Assert.Equal(OwnerId, stored.UserId);
        Assert.Equal(OrgId, stored.OrganizationId);
        Assert.Equal(ChatSession.StatusActive, stored.Status);
        Assert.Equal("Test session", stored.Title);
    }

    [Fact]
    public async Task CreateSession_DoesNotTriggerAiOrRetrieval()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);

        // Creating a session must not call the retrieval or AI services.
        await harness.Service.CreateSessionAsync(null);

        // No AI or retrieval audit events should be present.
        Assert.DoesNotContain(harness.Audit.Events,
            e => e.EventType is AuditEventTypes.ChatInteractionStarted
                              or AuditEventTypes.ChatAnswerGenerationCompleted
                              or AuditEventTypes.EligibleSemanticRetrievalCompleted);
    }

    // ── GetSession ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSession_OwnerCanViewOwnSession()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);
        harness.Sessions.Add(MakeSessionWithId(SessionId, OwnerId, OrgId));

        var result = await harness.Service.GetSessionAsync(SessionId);

        Assert.NotNull(result);
        Assert.Equal(SessionId, result.ChatSessionId);
    }

    [Fact]
    public async Task GetSession_AgentCannotViewOtherUsersSession()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);
        harness.Sessions.Add(MakeSessionWithId(SessionId, OtherId, OrgId));

        var result = await harness.Service.GetSessionAsync(SessionId);

        Assert.Null(result);
        Assert.Contains(harness.Audit.Events, e => e.EventType == AuditEventTypes.ChatHistoryDenied);
    }

    [Fact]
    public async Task GetSession_KnowledgeAdminCannotViewOtherUsersSession()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["KnowledgeAdmin"]);
        harness.Sessions.Add(MakeSessionWithId(SessionId, OtherId, OrgId));

        var result = await harness.Service.GetSessionAsync(SessionId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSession_SupervisorCanViewSameOrgSession()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Supervisor"]);
        harness.Sessions.Add(MakeSessionWithId(SessionId, OtherId, OrgId));

        var result = await harness.Service.GetSessionAsync(SessionId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetSession_CrossOrgReturnsNull()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Admin"]);
        harness.Sessions.Add(MakeSessionWithId(SessionId, OwnerId, OtherOrgId));

        var result = await harness.Service.GetSessionAsync(SessionId);

        Assert.Null(result);
    }

    // ── GetInteraction ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetInteraction_OwnerCanViewOwnInteraction()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);
        harness.Interactions.Add(MakeInteraction(InteractionId, SessionId, OwnerId, OrgId, AnswerState.Grounded));

        var result = await harness.Service.GetInteractionAsync(InteractionId);

        Assert.NotNull(result);
        Assert.Equal(InteractionId, result.ChatInteractionId);
        Assert.Equal("GroundedAnswer", result.AnswerState);
        Assert.False(result.InsufficientContext);
    }

    [Fact]
    public async Task GetInteraction_InsufficientContextState_MapsCorrectly()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);
        harness.Interactions.Add(MakeInteraction(InteractionId, SessionId, OwnerId, OrgId, AnswerState.InsufficientContext));

        var result = await harness.Service.GetInteractionAsync(InteractionId);

        Assert.NotNull(result);
        Assert.Equal("InsufficientContext", result.AnswerState);
        Assert.True(result.InsufficientContext);
    }

    [Fact]
    public async Task GetInteraction_ProviderFailed_DoesNotReturnAnswerText()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);
        var interaction = MakeInteraction(InteractionId, SessionId, OwnerId, OrgId, AnswerState.ProviderFailed);
        harness.Interactions.Add(interaction);

        var result = await harness.Service.GetInteractionAsync(InteractionId);

        Assert.NotNull(result);
        Assert.Null(result.AnswerText);
    }

    [Fact]
    public async Task GetInteraction_AgentCannotViewOtherUsersInteraction()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);
        harness.Interactions.Add(MakeInteraction(InteractionId, SessionId, OtherId, OrgId, AnswerState.Grounded));

        var result = await harness.Service.GetInteractionAsync(InteractionId);

        Assert.Null(result);
        Assert.Contains(harness.Audit.Events, e => e.EventType == AuditEventTypes.ChatHistoryDenied);
    }

    [Fact]
    public async Task GetInteraction_KnowledgeAdminCannotViewOtherUsersInteraction()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["KnowledgeAdmin"]);
        harness.Interactions.Add(MakeInteraction(InteractionId, SessionId, OtherId, OrgId, AnswerState.Grounded));

        var result = await harness.Service.GetInteractionAsync(InteractionId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetInteraction_SupervisorCanViewSameOrgInteraction()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Supervisor"]);
        harness.Interactions.Add(MakeInteraction(InteractionId, SessionId, OtherId, OrgId, AnswerState.Grounded));

        var result = await harness.Service.GetInteractionAsync(InteractionId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetInteraction_ManagerCanViewSameOrgInteraction()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Manager"]);
        harness.Interactions.Add(MakeInteraction(InteractionId, SessionId, OtherId, OrgId, AnswerState.Grounded));

        var result = await harness.Service.GetInteractionAsync(InteractionId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetInteraction_CrossOrgReturnsNull()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Admin"]);
        harness.Interactions.Add(MakeInteraction(InteractionId, SessionId, OwnerId, OtherOrgId, AnswerState.Grounded));

        var result = await harness.Service.GetInteractionAsync(InteractionId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetInteraction_IncludesCitations_OrderedByRank()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);
        harness.Interactions.Add(MakeInteraction(InteractionId, SessionId, OwnerId, OrgId, AnswerState.Grounded));
        harness.Citations.Add(MakeCitation(Guid.NewGuid(), InteractionId, OrgId, rank: 2));
        harness.Citations.Add(MakeCitation(Guid.NewGuid(), InteractionId, OrgId, rank: 1));

        var result = await harness.Service.GetInteractionAsync(InteractionId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Citations.Count);
        Assert.Equal(1, result.Citations[0].Rank);
        Assert.Equal(2, result.Citations[1].Rank);
    }

    // ── GetInteractionCitations ──────────────────────────────────────────────

    [Fact]
    public async Task GetInteractionCitations_OwnerCanViewCitations()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);
        harness.Interactions.Add(MakeInteraction(InteractionId, SessionId, OwnerId, OrgId, AnswerState.Grounded));
        harness.Citations.Add(MakeCitation(Guid.NewGuid(), InteractionId, OrgId, rank: 1));

        var result = await harness.Service.GetInteractionCitationsAsync(InteractionId);

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetInteractionCitations_NonOwnerAgentReturnsNull()
    {
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);
        harness.Interactions.Add(MakeInteraction(InteractionId, SessionId, OtherId, OrgId, AnswerState.Grounded));
        harness.Citations.Add(MakeCitation(Guid.NewGuid(), InteractionId, OrgId, rank: 1));

        var result = await harness.Service.GetInteractionCitationsAsync(InteractionId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetInteractionCitations_HistoricalCitationsVisibleEvenIfDocumentDisabled()
    {
        // Citations stored at interaction time remain visible regardless of source document state.
        // Citation data stored in the citations table has no dependency on document.is_retrieval_enabled.
        var harness = CreateHarness(userId: OwnerId, orgId: OrgId, roles: ["Agent"]);
        harness.Interactions.Add(MakeInteraction(InteractionId, SessionId, OwnerId, OrgId, AnswerState.Grounded));
        harness.Citations.Add(MakeCitation(Guid.NewGuid(), InteractionId, OrgId, rank: 1));

        var result = await harness.Service.GetInteractionCitationsAsync(InteractionId);

        Assert.NotNull(result);
        Assert.Single(result);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private sealed record TestHarness(
        ChatHistoryService Service,
        List<ChatSession> Sessions,
        List<ChatInteraction> Interactions,
        List<Citation> Citations,
        CapturingAuditEventWriter Audit);

    private static TestHarness CreateHarness(Guid userId, Guid orgId, IReadOnlyList<string> roles)
    {
        var sessions = new List<ChatSession>();
        var interactions = new List<ChatInteraction>();
        var citations = new List<Citation>();
        var audit = new CapturingAuditEventWriter();

        var service = new ChatHistoryService(
            new FakeCurrentUser(userId, orgId),
            new FakeAccessStateReader(userId, orgId, roles),
            new PermissionService(),
            new FakeSessionRepository(sessions, interactions),
            new FakeInteractionRepository(interactions),
            new FakeCitationRepository(citations),
            audit,
            new StaticCorrelationContext(),
            NullLogger<ChatHistoryService>.Instance);

        return new TestHarness(service, sessions, interactions, citations, audit);
    }

    private static ChatSession MakeSession(Guid userId, Guid orgId)
    {
        var s = ChatSession.Create(orgId, userId, null);
        return s;
    }

    private static ChatSession MakeSessionWithId(Guid id, Guid userId, Guid orgId)
    {
        var s = ChatSession.Create(orgId, userId, null);
        // Use reflection to set the Id since it's init-only
        typeof(ChatSession).GetProperty("Id")!.SetValue(s, id);
        return s;
    }

    private static ChatInteraction MakeInteraction(
        Guid id, Guid sessionId, Guid userId, Guid orgId, AnswerState answerState)
    {
        var i = ChatInteraction.Create(sessionId, orgId, userId, "test question", "hash", "corr");
        if (answerState == AnswerState.InsufficientContext)
            i.RecordInsufficientContextOutcome(null, 0, null, null);
        else if (answerState == AnswerState.ProviderFailed)
            i.RecordProviderFailedOutcome("safe-code", null, 0, null, null, null);
        else if (answerState == AnswerState.Grounded)
            i.RecordGroundedOutcome("answer text", null, 1, null, null, null, null, null, null, null, null);

        typeof(ChatInteraction).GetProperty("Id")!.SetValue(i, id);
        return i;
    }

    private static Citation MakeCitation(Guid id, Guid interactionId, Guid orgId, int rank) =>
        Citation.Create(interactionId, orgId,
            Guid.NewGuid(), Guid.NewGuid(), rank, "Test Doc", null, null, 0.9);

    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeCurrentUser(Guid userId, Guid orgId) : ICurrentUser
    {
        public Guid UserId => userId;
        public Guid OrganizationId => orgId;
        public string Email => "test@example.com";
        public string DisplayName => "Test User";
        public IReadOnlyList<string> Roles => [];
        public bool IsAuthenticated => true;
    }

    private sealed class FakeAccessStateReader(
        Guid userId, Guid orgId, IReadOnlyList<string> roles) : IUserAccessStateReader
    {
        public Task<UserAccessState?> FindActiveByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<UserAccessState?>(new UserAccessState(userId, orgId, roles));
    }

    private sealed class FakeSessionRepository(
        List<ChatSession> sessions,
        List<ChatInteraction> interactions) : IChatSessionRepository
    {
        public Task<ChatSession?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(sessions.FirstOrDefault(s => s.Id == id));

        public Task<ChatSession?> FindByIdAndOrganizationAsync(Guid id, Guid orgId, CancellationToken ct = default) =>
            Task.FromResult(sessions.FirstOrDefault(s => s.Id == id && s.OrganizationId == orgId));

        public Task<IReadOnlyList<ChatSession>> GetRecentByUserAsync(
            Guid userId, Guid orgId, int limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ChatSession>>(
                sessions.Where(s => s.UserId == userId && s.OrganizationId == orgId).Take(limit).ToList());

        public Task<IReadOnlyList<ChatSession>> GetRecentByOrganizationAsync(
            Guid orgId, int limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ChatSession>>(
                sessions.Where(s => s.OrganizationId == orgId).Take(limit).ToList());

        public Task<int> CountInteractionsBySessionAsync(Guid sessionId, CancellationToken ct = default) =>
            Task.FromResult(interactions.Count(i => i.ChatSessionId == sessionId));

        public Task AddAsync(ChatSession session, CancellationToken ct = default)
        {
            sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeInteractionRepository(List<ChatInteraction> interactions) : IChatInteractionRepository
    {
        public Task<ChatInteraction?> FindByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(interactions.FirstOrDefault(i => i.Id == id && i.OrganizationId == organizationId));

        public Task<IReadOnlyList<ChatInteraction>> GetBySessionIdAsync(
            Guid sessionId, Guid orgId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ChatInteraction>>(
                interactions.Where(i => i.ChatSessionId == sessionId && i.OrganizationId == orgId).ToList());

        public Task AddAsync(ChatInteraction interaction, CancellationToken ct = default)
        {
            interactions.Add(interaction);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeCitationRepository(List<Citation> citations) : ICitationRepository
    {
        public Task<IReadOnlyList<Citation>> GetByInteractionIdAsync(
            Guid interactionId, Guid orgId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Citation>>(
                citations.Where(c => c.ChatInteractionId == interactionId && c.OrganizationId == orgId).ToList());

        public Task AddRangeAsync(IReadOnlyList<Citation> toAdd, CancellationToken ct = default)
        {
            citations.AddRange(toAdd);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class CapturingAuditEventWriter : IAuditEventWriter
    {
        private readonly List<AuditEvent> _events = [];
        public IReadOnlyList<AuditEvent> Events => _events;

        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            _events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class StaticCorrelationContext : ICorrelationContext
    {
        public string CorrelationId => "test-history-correlation";
    }
}

using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Dashboard;
using KnowledgeOps.Application.Errors;

namespace KnowledgeOps.Application.Tests.Dashboard;

public sealed class DashboardServiceTests
{
    private static readonly Guid OrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherOrgId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DashboardDateRange DefaultRange = DashboardDateRange.CreateDefault();

    // ── Overview ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Overview_ReturnsOrganizationScopedCounts()
    {
        var repo = new FakeRepository();
        repo.OverviewResult = new DashboardOverviewResult(10, 3, 5, 4, 1, 200L, 1, 0, 7, 2, false, null);
        var svc = CreateService(repo, ["Manager"]);

        var result = await svc.GetOverviewAsync(DefaultRange);

        Assert.Equal(10, result.QuestionsAsked);
        Assert.Equal(3, result.ActiveUsers);
        Assert.Equal(5, result.DocumentsUploaded);
    }

    [Fact]
    public async Task Overview_ExcludesOtherOrganizationData()
    {
        // The service derives orgId from persisted state — not from caller input.
        // The repository receives the correct orgId; tests for cross-org exclusion live in integration tests.
        var repo = new FakeRepository();
        repo.OverviewResult = new DashboardOverviewResult(0, 0, 0, 0, 0, null, 0, 0, 0, 0, false, null);
        var svc = CreateService(repo, ["Manager"]);

        var result = await svc.GetOverviewAsync(DefaultRange);

        // Verify the org passed to repository matches the authenticated user's org
        Assert.Equal(OrgId, repo.LastOrganizationId);
    }

    [Fact]
    public async Task Overview_ReturnsNullCostWhenNoInteractionsHaveCost()
    {
        var repo = new FakeRepository();
        repo.OverviewResult = new DashboardOverviewResult(3, 1, 0, 0, 0, null, 0, 0, 0, 0, false, null);
        var svc = CreateService(repo, ["Manager"]);

        var result = await svc.GetOverviewAsync(DefaultRange);

        Assert.False(result.EstimatedCostAvailable);
        Assert.Null(result.EstimatedCostTotal);
    }

    [Fact]
    public async Task Overview_DoesNotReportNullCostAsZero()
    {
        var repo = new FakeRepository();
        repo.OverviewResult = new DashboardOverviewResult(1, 1, 0, 0, 0, null, 0, 0, 0, 0, false, null);
        var svc = CreateService(repo, ["Manager"]);

        var result = await svc.GetOverviewAsync(DefaultRange);

        // When available = false, cost must be null — never 0.
        Assert.False(result.EstimatedCostAvailable);
        Assert.Null(result.EstimatedCostTotal);
    }

    [Fact]
    public async Task Overview_RequiresDashboardViewOverviewPermission()
    {
        var repo = new FakeRepository();
        var svc = CreateService(repo, ["Agent"]);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(() =>
            svc.GetOverviewAsync(DefaultRange));
    }

    [Fact]
    public async Task Overview_KnowledgeAdmin_IsAllowed()
    {
        var repo = new FakeRepository();
        repo.OverviewResult = new DashboardOverviewResult(0, 0, 0, 0, 0, null, 0, 0, 0, 0, false, null);
        var svc = CreateService(repo, ["KnowledgeAdmin"]);

        var result = await svc.GetOverviewAsync(DefaultRange);

        Assert.NotNull(result);
    }

    // ── Documents ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Documents_CountsByStatus()
    {
        var repo = new FakeRepository();
        repo.DocumentsResult = new DashboardDocumentsResult(2, 1, 10, 3, 5);
        var svc = CreateService(repo, ["KnowledgeAdmin"]);

        var result = await svc.GetDocumentsAsync(DefaultRange);

        Assert.Equal(2, result.Uploaded);
        Assert.Equal(1, result.Processing);
        Assert.Equal(10, result.Processed);
        Assert.Equal(3, result.Failed);
    }

    [Fact]
    public async Task Documents_ExcludesSoftDeletedDocuments()
    {
        // Soft-delete exclusion is enforced in the repository (DeletedAt == null filter).
        // The service delegates to the repository; here we verify the call goes through correctly.
        var repo = new FakeRepository();
        repo.DocumentsResult = new DashboardDocumentsResult(0, 0, 5, 0, 0);
        var svc = CreateService(repo, ["KnowledgeAdmin"]);

        var result = await svc.GetDocumentsAsync(DefaultRange);

        Assert.Equal(OrgId, repo.LastOrganizationId);
        Assert.Equal(5, result.Processed);
    }

    [Fact]
    public async Task Documents_CountsRetrievalDisabled()
    {
        var repo = new FakeRepository();
        repo.DocumentsResult = new DashboardDocumentsResult(0, 0, 10, 0, 4);
        var svc = CreateService(repo, ["KnowledgeAdmin"]);

        var result = await svc.GetDocumentsAsync(DefaultRange);

        Assert.Equal(4, result.RetrievalDisabled);
    }

    [Fact]
    public async Task Documents_Agent_IsDenied()
    {
        var repo = new FakeRepository();
        var svc = CreateService(repo, ["Agent"]);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(() =>
            svc.GetDocumentsAsync(DefaultRange));
    }

    // ── Chat ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Chat_AverageLatencyFromNonNullRecordsOnly()
    {
        var repo = new FakeRepository();
        repo.ChatResult = new DashboardChatResult(5, 2, 250L, 80L, 170L, 250L, 0, 0, null, null, false, null);
        var svc = CreateService(repo, ["Manager"]);

        var result = await svc.GetChatAsync(DefaultRange);

        Assert.Equal(250L, result.AverageResponseLatencyMs);
        Assert.Equal(80L, result.RetrievalLatencyMs);
        Assert.Equal(170L, result.GenerationLatencyMs);
    }

    [Fact]
    public async Task Chat_AverageLatencyIsNullWhenAllNull()
    {
        var repo = new FakeRepository();
        repo.ChatResult = new DashboardChatResult(3, 1, null, null, null, null, 0, 0, null, null, false, null);
        var svc = CreateService(repo, ["Manager"]);

        var result = await svc.GetChatAsync(DefaultRange);

        Assert.Null(result.AverageResponseLatencyMs);
        Assert.Null(result.RetrievalLatencyMs);
        Assert.Null(result.GenerationLatencyMs);
    }

    [Fact]
    public async Task Chat_TokenSumsFromNonNullRecordsOnly()
    {
        var repo = new FakeRepository();
        repo.ChatResult = new DashboardChatResult(5, 2, null, null, null, null, 0, 0, 1000L, 500L, false, null);
        var svc = CreateService(repo, ["Manager"]);

        var result = await svc.GetChatAsync(DefaultRange);

        Assert.Equal(1000L, result.TokenInputTotal);
        Assert.Equal(500L, result.TokenOutputTotal);
    }

    [Fact]
    public async Task Chat_InsufficientContextCount_DerivedFromAnswerState()
    {
        var repo = new FakeRepository();
        repo.ChatResult = new DashboardChatResult(10, 3, null, null, null, null, 2, 1, null, null, false, null);
        var svc = CreateService(repo, ["Manager"]);

        var result = await svc.GetChatAsync(DefaultRange);

        Assert.Equal(2, result.InsufficientContextCount);
    }

    [Fact]
    public async Task Chat_ProviderFailureCount_DerivedFromAnswerState()
    {
        var repo = new FakeRepository();
        repo.ChatResult = new DashboardChatResult(10, 3, null, null, null, null, 2, 3, null, null, false, null);
        var svc = CreateService(repo, ["Manager"]);

        var result = await svc.GetChatAsync(DefaultRange);

        Assert.Equal(3, result.ProviderFailureCount);
    }

    [Fact]
    public async Task Chat_Agent_IsDenied()
    {
        var repo = new FakeRepository();
        var svc = CreateService(repo, ["Agent"]);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(() =>
            svc.GetChatAsync(DefaultRange));
    }

    [Fact]
    public async Task Chat_KnowledgeAdmin_IsDenied()
    {
        var repo = new FakeRepository();
        var svc = CreateService(repo, ["KnowledgeAdmin"]);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(() =>
            svc.GetChatAsync(DefaultRange));
    }

    // ── Feedback ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Feedback_UsefulAndNotUsefulCounts()
    {
        var repo = new FakeRepository();
        repo.FeedbackResult = new DashboardFeedbackResult(7, 3);
        var svc = CreateService(repo, ["Supervisor"]);

        var result = await svc.GetFeedbackAsync(DefaultRange);

        Assert.Equal(7, result.Useful);
        Assert.Equal(3, result.NotUseful);
    }

    [Fact]
    public async Task Feedback_CountsOrganizationScoped()
    {
        var repo = new FakeRepository();
        repo.FeedbackResult = new DashboardFeedbackResult(4, 1);
        var svc = CreateService(repo, ["Supervisor"]);

        var result = await svc.GetFeedbackAsync(DefaultRange);

        Assert.Equal(OrgId, repo.LastOrganizationId);
    }

    [Fact]
    public async Task Feedback_Agent_IsDenied()
    {
        var repo = new FakeRepository();
        var svc = CreateService(repo, ["Agent"]);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(() =>
            svc.GetFeedbackAsync(DefaultRange));
    }

    [Fact]
    public async Task Feedback_Supervisor_IsAllowed()
    {
        var repo = new FakeRepository();
        repo.FeedbackResult = new DashboardFeedbackResult(1, 0);
        var svc = CreateService(repo, ["Supervisor"]);

        var result = await svc.GetFeedbackAsync(DefaultRange);

        Assert.NotNull(result);
    }

    // ── DashboardDateRange ────────────────────────────────────────────────────

    [Fact]
    public void DateRange_CreateDefault_Returns30DayPeriod()
    {
        var range = DashboardDateRange.CreateDefault();

        var diff = range.To - range.From;
        Assert.True(diff.TotalDays >= 29.99 && diff.TotalDays <= 30.01);
    }

    [Fact]
    public void DateRange_Create_BothProvided_UsesBoth()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var range = DashboardDateRange.Create(from, to);

        Assert.Equal(new DateTimeOffset(from, TimeSpan.Zero), range.From);
        Assert.Equal(new DateTimeOffset(to, TimeSpan.Zero), range.To);
    }

    [Fact]
    public void DateRange_Create_OnlyFrom_UsesNowAsTo()
    {
        var from = DateTime.UtcNow.AddDays(-10);
        var range = DashboardDateRange.Create(from, null);

        Assert.Equal(new DateTimeOffset(from, TimeSpan.Zero), range.From);
        Assert.True(range.To >= range.From);
    }

    [Fact]
    public void DateRange_Create_OnlyTo_Uses30DaysBeforeTo()
    {
        var to = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);
        var range = DashboardDateRange.Create(null, to);

        var expected = new DateTimeOffset(to, TimeSpan.Zero).AddDays(-30);
        Assert.Equal(expected, range.From);
        Assert.Equal(new DateTimeOffset(to, TimeSpan.Zero), range.To);
    }

    [Fact]
    public void DateRange_Create_FromGreaterThanTo_Throws()
    {
        var from = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() => DashboardDateRange.Create(from, to));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DashboardService CreateService(FakeRepository repo, IReadOnlyList<string> roles)
    {
        var state = new UserAccessState(UserId, OrgId, roles);
        return new DashboardService(
            new FakeCurrentUser(UserId),
            new FakeAccessStateReader(state),
            new PermissionService(),
            repo);
    }

    private sealed class FakeRepository : IDashboardRepository
    {
        public Guid LastOrganizationId { get; private set; }
        public DashboardOverviewResult OverviewResult { get; set; } =
            new DashboardOverviewResult(0, 0, 0, 0, 0, null, 0, 0, 0, 0, false, null);
        public DashboardDocumentsResult DocumentsResult { get; set; } =
            new DashboardDocumentsResult(0, 0, 0, 0, 0);
        public DashboardChatResult ChatResult { get; set; } =
            new DashboardChatResult(0, 0, null, null, null, null, 0, 0, null, null, false, null);
        public DashboardFeedbackResult FeedbackResult { get; set; } =
            new DashboardFeedbackResult(0, 0);

        public Task<DashboardOverviewResult> GetOverviewAsync(Guid organizationId, DashboardDateRange range, CancellationToken ct = default)
        {
            LastOrganizationId = organizationId;
            return Task.FromResult(OverviewResult);
        }

        public Task<DashboardDocumentsResult> GetDocumentsAsync(Guid organizationId, DashboardDateRange range, CancellationToken ct = default)
        {
            LastOrganizationId = organizationId;
            return Task.FromResult(DocumentsResult);
        }

        public Task<DashboardChatResult> GetChatAsync(Guid organizationId, DashboardDateRange range, CancellationToken ct = default)
        {
            LastOrganizationId = organizationId;
            return Task.FromResult(ChatResult);
        }

        public Task<DashboardFeedbackResult> GetFeedbackAsync(Guid organizationId, DashboardDateRange range, CancellationToken ct = default)
        {
            LastOrganizationId = organizationId;
            return Task.FromResult(FeedbackResult);
        }
    }

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId => userId;
        public Guid OrganizationId => OrgId;
        public string Email => "user@example.test";
        public string DisplayName => "Test User";
        public IReadOnlyList<string> Roles => [];
        public bool IsAuthenticated => true;
    }

    private sealed class FakeAccessStateReader(UserAccessState? state) : IUserAccessStateReader
    {
        public Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(state);
    }
}

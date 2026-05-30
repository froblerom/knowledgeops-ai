using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Chat.Feedback;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Chat;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Chat.Feedback;

public sealed class AnswerFeedbackServiceTests
{
    private static readonly Guid OrgId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid OtherOrgId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid OtherUserId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid InteractionId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");

    [Theory]
    [InlineData(AnswerFeedbackRating.Useful)]
    [InlineData(AnswerFeedbackRating.NotUseful)]
    public async Task SubmitAsync_StoresUsefulAndNotUsefulFeedback(AnswerFeedbackRating rating)
    {
        var harness = Harness(["Agent"]);
        harness.Repository.Interactions.Add(Interaction(UserId, OrgId));

        var result = await harness.Service.SubmitAsync(new SubmitAnswerFeedbackRequest(InteractionId, rating));

        Assert.Equal(rating, result.Rating);
        Assert.Single(harness.Repository.Feedback);
        Assert.Equal(OrgId, harness.Repository.Feedback[0].OrganizationId);
        Assert.Equal(UserId, harness.Repository.Feedback[0].UserId);
        Assert.Equal(InteractionId, harness.Repository.Feedback[0].ChatInteractionId);
    }

    [Fact]
    public async Task UpdateOwnAsync_ChangesOwnExistingFeedbackWithoutCreatingAnotherRow()
    {
        var harness = Harness(["Agent"]);
        harness.Repository.Interactions.Add(Interaction(UserId, OrgId));
        var feedback = AnswerFeedback.Create(OrgId, UserId, InteractionId, AnswerFeedbackRating.Useful);
        harness.Repository.Feedback.Add(feedback);

        var result = await harness.Service.UpdateOwnAsync(
            new UpdateAnswerFeedbackRequest(InteractionId, AnswerFeedbackRating.NotUseful));

        Assert.Equal(AnswerFeedbackRating.NotUseful, result.Rating);
        Assert.Single(harness.Repository.Feedback);
        Assert.Equal(AnswerFeedbackRating.NotUseful, harness.Repository.Feedback[0].Rating);
    }

    [Fact]
    public async Task SubmitAsync_DuplicateFeedbackConflictsAndDoesNotInflateMetrics()
    {
        var harness = Harness(["Agent"]);
        harness.Repository.Interactions.Add(Interaction(UserId, OrgId));
        harness.Repository.Feedback.Add(AnswerFeedback.Create(OrgId, UserId, InteractionId, AnswerFeedbackRating.Useful));

        await Assert.ThrowsAsync<ApplicationConflictException>(() =>
            harness.Service.SubmitAsync(new SubmitAnswerFeedbackRequest(InteractionId, AnswerFeedbackRating.NotUseful)));

        Assert.Single(harness.Repository.Feedback);
        var review = await ReviewerHarness(harness.Repository, ["Supervisor"]).Service.GetReviewDataAsync();
        Assert.Equal(1, review.UsefulCount);
        Assert.Equal(0, review.NotUsefulCount);
    }

    [Fact]
    public async Task SubmitAsync_DeniesCrossOrganizationInteraction()
    {
        var harness = Harness(["Agent"]);
        harness.Repository.Interactions.Add(Interaction(UserId, OtherOrgId));

        await Assert.ThrowsAsync<ApplicationNotFoundException>(() =>
            harness.Service.SubmitAsync(new SubmitAnswerFeedbackRequest(InteractionId, AnswerFeedbackRating.Useful)));

        Assert.Empty(harness.Repository.Feedback);
    }

    [Fact]
    public async Task SubmitAsync_DeniesSameOrganizationInteractionWithoutOwnOrScopedAccess()
    {
        var harness = Harness(["Agent"]);
        harness.Repository.Interactions.Add(Interaction(OtherUserId, OrgId));

        await Assert.ThrowsAsync<ApplicationNotFoundException>(() =>
            harness.Service.SubmitAsync(new SubmitAnswerFeedbackRequest(InteractionId, AnswerFeedbackRating.Useful)));
    }

    [Fact]
    public async Task SubmitAsync_AllowsSameOrganizationScopedHistoryReviewer()
    {
        var harness = Harness(["Supervisor"]);
        harness.Repository.Interactions.Add(Interaction(OtherUserId, OrgId));

        var result = await harness.Service.SubmitAsync(
            new SubmitAnswerFeedbackRequest(InteractionId, AnswerFeedbackRating.NotUseful));

        Assert.Equal(UserId, result.UserId);
        Assert.Equal(AnswerFeedbackRating.NotUseful, result.Rating);
    }

    [Fact]
    public async Task UpdateOwnAsync_DeniesNonOwnerUpdate()
    {
        var harness = Harness(["Agent"]);
        harness.Repository.Interactions.Add(Interaction(UserId, OrgId));
        var feedback = AnswerFeedback.Create(OrgId, OtherUserId, InteractionId, AnswerFeedbackRating.Useful);
        harness.Repository.Feedback.Add(feedback);

        await Assert.ThrowsAsync<ApplicationNotFoundException>(() =>
            harness.Service.UpdateOwnAsync(new UpdateAnswerFeedbackRequest(InteractionId, AnswerFeedbackRating.NotUseful)));
    }

    [Fact]
    public async Task UpdateOwnAsync_DeniesCrossOrganizationInteraction()
    {
        var harness = Harness(["Agent"]);
        harness.Repository.Interactions.Add(Interaction(UserId, OtherOrgId));
        harness.Repository.Feedback.Add(AnswerFeedback.Create(OtherOrgId, UserId, InteractionId, AnswerFeedbackRating.Useful));

        await Assert.ThrowsAsync<ApplicationNotFoundException>(() =>
            harness.Service.UpdateOwnAsync(new UpdateAnswerFeedbackRequest(InteractionId, AnswerFeedbackRating.NotUseful)));
    }

    [Fact]
    public async Task GetReviewDataAsync_RequiresFeedbackReviewPermission()
    {
        var harness = Harness(["Agent"]);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(() =>
            harness.Service.GetReviewDataAsync());
    }

    [Fact]
    public async Task GetReviewDataAsync_ReturnsOnlyOrganizationScopedSimpleSignals()
    {
        var repository = new FakeRepository();
        repository.Feedback.Add(AnswerFeedback.Create(OrgId, UserId, InteractionId, AnswerFeedbackRating.Useful));
        repository.Feedback.Add(AnswerFeedback.Create(OrgId, OtherUserId, Guid.NewGuid(), AnswerFeedbackRating.NotUseful));
        repository.Feedback.Add(AnswerFeedback.Create(OtherOrgId, OtherUserId, Guid.NewGuid(), AnswerFeedbackRating.NotUseful));
        var harness = ReviewerHarness(repository, ["Manager"]);

        var result = await harness.Service.GetReviewDataAsync();

        Assert.Equal(1, result.UsefulCount);
        Assert.Equal(1, result.NotUsefulCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, i =>
            Assert.Equal(OrgId, repository.Feedback.Single(f => f.Id == i.FeedbackId).OrganizationId));
    }

    [Fact]
    public async Task FeedbackAuditMessages_DoNotContainSensitiveContent()
    {
        var harness = Harness(["Agent"]);
        harness.Repository.Interactions.Add(Interaction(UserId, OrgId));
        const string sensitiveAnswer = "answer secret prompt chunks provider payload token";

        await harness.Service.SubmitAsync(new SubmitAnswerFeedbackRequest(InteractionId, AnswerFeedbackRating.Useful));

        var messages = string.Join("|", harness.Audit.Events.Select(e => e.Message));
        Assert.Contains(AuditEventTypes.FeedbackSubmitted, harness.Audit.Events.Select(e => e.EventType));
        Assert.All(harness.Audit.Events, e => Assert.Equal("Feedback", e.EntityType));
        foreach (var term in sensitiveAnswer.Split(' '))
        {
            Assert.DoesNotContain(term, messages, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static HarnessContext Harness(IReadOnlyList<string> roles)
    {
        var repository = new FakeRepository();
        return ReviewerHarness(repository, roles);
    }

    private static HarnessContext ReviewerHarness(FakeRepository repository, IReadOnlyList<string> roles)
    {
        var currentUser = new FakeCurrentUser(UserId, OrgId, roles);
        var audit = new RecordingAuditWriter();
        var service = new AnswerFeedbackService(
            currentUser,
            new FakeAccessStateReader(new UserAccessState(UserId, OrgId, roles)),
            new PermissionService(),
            repository,
            audit,
            new FixedCorrelationContext(),
            NullLogger<AnswerFeedbackService>.Instance);

        return new HarnessContext(service, repository, audit);
    }

    private static ChatInteraction Interaction(Guid userId, Guid organizationId) =>
        new()
        {
            Id = InteractionId,
            ChatSessionId = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            QuestionText = "What is the sensitive question?",
            QuestionTextHash = new string('a', 64),
            CorrelationId = "corr",
            CreatedAt = DateTimeOffset.UtcNow,
        };

    private sealed record HarnessContext(
        AnswerFeedbackService Service,
        FakeRepository Repository,
        RecordingAuditWriter Audit);

    private sealed class FakeRepository : IAnswerFeedbackRepository
    {
        public List<ChatInteraction> Interactions { get; } = [];
        public List<AnswerFeedback> Feedback { get; } = [];

        public Task<ChatInteraction?> FindInteractionAsync(Guid interactionId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(Interactions.SingleOrDefault(i => i.Id == interactionId && i.OrganizationId == organizationId));

        public Task<AnswerFeedback?> FindByIdAsync(Guid feedbackId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(Feedback.SingleOrDefault(f => f.Id == feedbackId && f.OrganizationId == organizationId));

        public Task<AnswerFeedback?> FindByInteractionAndUserAsync(
            Guid interactionId,
            Guid userId,
            Guid organizationId,
            CancellationToken ct = default) =>
            Task.FromResult(Feedback.SingleOrDefault(f =>
                f.ChatInteractionId == interactionId
                && f.UserId == userId
                && f.OrganizationId == organizationId));

        public Task<IReadOnlyList<AnswerFeedback>> ListForReviewAsync(Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AnswerFeedback>>(Feedback.Where(f => f.OrganizationId == organizationId).ToArray());

        public Task AddAsync(AnswerFeedback feedback, CancellationToken ct = default)
        {
            Feedback.Add(feedback);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeCurrentUser(
        Guid userId,
        Guid organizationId,
        IReadOnlyList<string> roles) : ICurrentUser
    {
        public Guid UserId => userId;
        public Guid OrganizationId => organizationId;
        public string Email => "agent@example.test";
        public string DisplayName => "Agent";
        public IReadOnlyList<string> Roles => roles;
        public bool IsAuthenticated => true;
    }

    private sealed class FakeAccessStateReader(UserAccessState? state) : IUserAccessStateReader
    {
        public Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(state);
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

    private sealed class FixedCorrelationContext : ICorrelationContext
    {
        public string CorrelationId => "feedback-correlation";
    }
}

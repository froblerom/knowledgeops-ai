using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Domain.Tests.Chat;

public sealed class AnswerFeedbackTests
{
    private static readonly Guid InteractionId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid UserId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid OrganizationId = Guid.Parse("44444444-4444-4444-8444-444444444444");

    [Fact]
    public void Create_CapturesUsefulFeedbackScopeAndTimestamps()
    {
        var feedback = AnswerFeedback.Create(
            OrganizationId,
            UserId,
            InteractionId,
            AnswerFeedbackRating.Useful);

        Assert.NotEqual(Guid.Empty, feedback.Id);
        Assert.Equal(InteractionId, feedback.ChatInteractionId);
        Assert.Equal(UserId, feedback.UserId);
        Assert.Equal(OrganizationId, feedback.OrganizationId);
        Assert.Equal(AnswerFeedbackRating.Useful, feedback.Rating);
        Assert.NotEqual(default, feedback.CreatedAt);
        Assert.Equal(feedback.CreatedAt, feedback.UpdatedAt);
    }

    [Fact]
    public void UpdateRating_ChangesOnlyRatingAndUpdatedAt()
    {
        var feedback = AnswerFeedback.Create(
            OrganizationId,
            UserId,
            InteractionId,
            AnswerFeedbackRating.Useful);
        var createdAt = feedback.CreatedAt;

        feedback.UpdateRating(AnswerFeedbackRating.NotUseful);

        Assert.Equal(AnswerFeedbackRating.NotUseful, feedback.Rating);
        Assert.Equal(createdAt, feedback.CreatedAt);
        Assert.True(feedback.UpdatedAt >= createdAt);
        Assert.Equal(InteractionId, feedback.ChatInteractionId);
        Assert.Equal(UserId, feedback.UserId);
        Assert.Equal(OrganizationId, feedback.OrganizationId);
    }

    [Theory]
    [InlineData("chatInteractionId")]
    [InlineData("userId")]
    [InlineData("organizationId")]
    public void Create_RequiresScopedIdentifiers(string missing)
    {
        var interactionId = missing == "chatInteractionId" ? Guid.Empty : InteractionId;
        var userId = missing == "userId" ? Guid.Empty : UserId;
        var organizationId = missing == "organizationId" ? Guid.Empty : OrganizationId;

        Assert.Throws<ArgumentException>(() =>
            AnswerFeedback.Create(
                organizationId,
                userId,
                interactionId,
                AnswerFeedbackRating.Useful));
    }
}

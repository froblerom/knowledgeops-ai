using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Domain.Tests.Chat;

public sealed class ChatInteractionTests
{
    private static readonly Guid SessionId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid OrganizationId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid UserId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    [Fact]
    public void Create_StoresQuestionTraceAndStartsGrounded()
    {
        var interaction = ChatInteraction.Create(
            SessionId,
            OrganizationId,
            UserId,
            "What is the refund policy?",
            "hash",
            "corr-1");

        Assert.Equal(SessionId, interaction.ChatSessionId);
        Assert.Equal(OrganizationId, interaction.OrganizationId);
        Assert.Equal(UserId, interaction.UserId);
        Assert.Equal("What is the refund policy?", interaction.QuestionText);
        Assert.Equal("hash", interaction.QuestionTextHash);
        Assert.Equal("corr-1", interaction.CorrelationId);
        Assert.Equal(AnswerState.Grounded, interaction.AnswerState);
        Assert.NotEqual(Guid.Empty, interaction.Id);
        Assert.NotEqual(default, interaction.CreatedAt);
    }

    [Fact]
    public void RecordGroundedOutcome_StoresAnswerAndMetrics()
    {
        var interaction = MakeInteraction();
        var retrievalQueryId = Guid.Parse("44444444-4444-4444-8444-444444444444");

        interaction.RecordGroundedOutcome(
            "Follow the documented escalation path.",
            retrievalQueryId,
            candidateCount: 3,
            retrievalMs: 25,
            generationMs: 80,
            totalMs: 105,
            inputTokens: 100,
            outputTokens: 42,
            cost: 0.0012m,
            provider: "Fake",
            model: "fake-deterministic-v1",
            promptVersion: "rag-grounded-v1");

        Assert.Equal(AnswerState.Grounded, interaction.AnswerState);
        Assert.Equal("Follow the documented escalation path.", interaction.AnswerText);
        Assert.Equal(retrievalQueryId, interaction.RetrievalQueryId);
        Assert.Equal(3, interaction.RetrievalCandidateCount);
        Assert.Equal(25, interaction.RetrievalLatencyMs);
        Assert.Equal(80, interaction.GenerationLatencyMs);
        Assert.Equal(105, interaction.TotalLatencyMs);
        Assert.Equal(100, interaction.TokenUsageInput);
        Assert.Equal(42, interaction.TokenUsageOutput);
        Assert.Equal(0.0012m, interaction.EstimatedCost);
        Assert.Equal("Fake", interaction.AiProvider);
        Assert.Equal("fake-deterministic-v1", interaction.AiModel);
        Assert.Equal("rag-grounded-v1", interaction.PromptVersion);
    }

    [Fact]
    public void RecordInsufficientContextOutcome_DoesNotStoreAnswerTextOrGenerationMetrics()
    {
        var interaction = MakeInteraction();
        var retrievalQueryId = Guid.Parse("55555555-5555-4555-8555-555555555555");

        interaction.RecordInsufficientContextOutcome(
            retrievalQueryId,
            candidateCount: 0,
            retrievalMs: 30,
            totalMs: 30);

        Assert.Equal(AnswerState.InsufficientContext, interaction.AnswerState);
        Assert.Null(interaction.AnswerText);
        Assert.Equal(retrievalQueryId, interaction.RetrievalQueryId);
        Assert.Equal(0, interaction.RetrievalCandidateCount);
        Assert.Equal(30, interaction.RetrievalLatencyMs);
        Assert.Null(interaction.GenerationLatencyMs);
        Assert.Equal(30, interaction.TotalLatencyMs);
        Assert.Null(interaction.TokenUsageInput);
        Assert.Null(interaction.TokenUsageOutput);
        Assert.Null(interaction.EstimatedCost);
    }

    [Fact]
    public void RecordProviderFailedOutcome_StoresFailureCodeAndProviderInfo()
    {
        var interaction = MakeInteraction();
        var retrievalQueryId = Guid.Parse("66666666-6666-4666-8666-666666666666");

        interaction.RecordProviderFailedOutcome(
            "ProviderUnavailable",
            retrievalQueryId,
            candidateCount: 3,
            retrievalMs: 20,
            generationMs: 40,
            totalMs: 60,
            aiProvider: "QwenLocal",
            aiModel: "qwen3:8b");

        Assert.Equal(AnswerState.ProviderFailed, interaction.AnswerState);
        Assert.Null(interaction.AnswerText);
        Assert.Equal("ProviderUnavailable", interaction.ProviderFailureCode);
        Assert.Equal("QwenLocal", interaction.AiProvider);
        Assert.Equal("qwen3:8b", interaction.AiModel);
        Assert.Equal(retrievalQueryId, interaction.RetrievalQueryId);
        Assert.Equal(3, interaction.RetrievalCandidateCount);
        Assert.Equal(20, interaction.RetrievalLatencyMs);
        Assert.Equal(40, interaction.GenerationLatencyMs);
        Assert.Equal(60, interaction.TotalLatencyMs);
    }

    [Fact]
    public void RecordProviderFailedOutcome_AcceptsNullProviderInfo()
    {
        var interaction = MakeInteraction();

        interaction.RecordProviderFailedOutcome(
            "RetrievalFailed",
            retrievalQueryId: null,
            candidateCount: 0,
            retrievalMs: 10,
            generationMs: null,
            totalMs: 10,
            aiProvider: null,
            aiModel: null);

        Assert.Equal(AnswerState.ProviderFailed, interaction.AnswerState);
        Assert.Equal("RetrievalFailed", interaction.ProviderFailureCode);
        Assert.Null(interaction.AiProvider);
        Assert.Null(interaction.AiModel);
    }

    private static ChatInteraction MakeInteraction() =>
        ChatInteraction.Create(
            SessionId,
            OrganizationId,
            UserId,
            "What is the refund policy?",
            "hash",
            "corr-1");
}

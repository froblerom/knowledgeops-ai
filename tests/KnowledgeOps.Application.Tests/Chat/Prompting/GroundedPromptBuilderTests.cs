using KnowledgeOps.Application.Authorization.Hooks;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Application.Chat.Prompting;

namespace KnowledgeOps.Application.Tests.Chat.Prompting;

public sealed class GroundedPromptBuilderTests
{
    private static readonly Guid OrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherOrgId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void GroundedPromptBuilder_AssemblesPromptFromAuthorizedChunks()
    {
        var builder = CreateBuilder(allowAll: true);
        var request = BuildRequest([MakeChunk(OrgId, "Policy section content.")]);

        var result = builder.Build(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.GroundedPrompt);
        Assert.Equal("rag-grounded-v1", result.GroundedPrompt!.PromptVersion);
        Assert.Contains("Policy section content.", result.GroundedPrompt.FormattedContext);
        Assert.Equal(1, result.IncludedChunkCount);
        Assert.Equal(0, result.ExcludedChunkCount);
    }

    [Fact]
    public void GroundedPromptBuilder_ExcludesChunksThatFailPromptAuthorizationFilter()
    {
        var builder = CreateBuilder(allowAll: false);
        var request = BuildRequest([MakeChunk(OrgId, "Secret content.")]);

        var result = builder.Build(request);

        Assert.False(result.IsSuccess);
        Assert.Null(result.GroundedPrompt);
        Assert.Equal("NoAuthorizedChunks", result.FailureCode);
        Assert.Equal(0, result.IncludedChunkCount);
        Assert.Equal(1, result.ExcludedChunkCount);
    }

    [Fact]
    public void GroundedPromptBuilder_ReturnsFailureResultWhenNoChunksPassFilter()
    {
        // Chunks belong to a different org; filter accepts only same-org chunks
        var builder = new GroundedPromptBuilder(new OrgScopePromptAuthorizationFilter());
        var request = new GroundedPromptBuildRequest(
            UserQuestion: "What is the policy?",
            OrganizationId: OrgId,
            AuthorizedChunks: [MakeChunk(OtherOrgId, "Cross-org content.")]);

        var result = builder.Build(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("NoAuthorizedChunks", result.FailureCode);
        Assert.Equal(0, result.IncludedChunkCount);
        Assert.Equal(1, result.ExcludedChunkCount);
    }

    [Fact]
    public void GroundedPromptBuilder_NeverIncludesFullQuestionTextInMetadataFields()
    {
        var builder = CreateBuilder(allowAll: true);
        const string sensitiveQuestion = "SensitiveQuestionText-12345";
        var request = new GroundedPromptBuildRequest(
            UserQuestion: sensitiveQuestion,
            OrganizationId: OrgId,
            AuthorizedChunks: [MakeChunk(OrgId, "Some approved context.")]);

        var result = builder.Build(request);

        Assert.True(result.IsSuccess);
        // GroundedPrompt.UserQuestion contains the question — that's expected in the prompt itself
        // But metadata fields (PromptVersion, FormattedContext) must not contain the raw question text
        Assert.DoesNotContain(sensitiveQuestion, result.GroundedPrompt!.PromptVersion, StringComparison.Ordinal);
        Assert.DoesNotContain(sensitiveQuestion, result.GroundedPrompt.SystemInstruction, StringComparison.Ordinal);
        // Source handles (FailureCode, counts) must not contain question text
        if (result.FailureCode is not null)
            Assert.DoesNotContain(sensitiveQuestion, result.FailureCode, StringComparison.Ordinal);
    }

    [Fact]
    public void ContextSufficiencyPolicy_ReturnsSufficientForNonEmptyCandidates()
    {
        var policy = new ContextSufficiencyPolicy();
        var result = policy.Evaluate([MakeChunk(OrgId, "Some content.")]);
        Assert.True(result.IsSufficient);
        Assert.Null(result.FailureCode);
    }

    [Fact]
    public void ContextSufficiencyPolicy_ReturnsInsufficientForEmptyCandidates()
    {
        var policy = new ContextSufficiencyPolicy();
        var result = policy.Evaluate([]);
        Assert.False(result.IsSufficient);
        Assert.Equal("NoAuthorizedChunks", result.FailureCode);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static IGroundedPromptBuilder CreateBuilder(bool allowAll) =>
        new GroundedPromptBuilder(new AllowOrDenyFilter(allowAll));

    private static GroundedPromptBuildRequest BuildRequest(
        IReadOnlyList<AuthorizedChunkContext> chunks) =>
        new(UserQuestion: "What is the policy?", OrganizationId: OrgId, AuthorizedChunks: chunks);

    private static AuthorizedChunkContext MakeChunk(Guid orgId, string text) =>
        new(
            ChunkId: Guid.NewGuid(),
            DocumentId: Guid.NewGuid(),
            OrganizationId: orgId,
            ChunkText: text,
            ChunkIndex: 0,
            PageNumber: 1,
            SectionLabel: "Policy");

    private sealed class AllowOrDenyFilter(bool allow) : IPromptAuthorizationFilter
    {
        public bool IsChunkAuthorizedForPrompt(Guid chunkOrg, Guid userOrg) => allow;
    }

    private sealed class OrgScopePromptAuthorizationFilter : IPromptAuthorizationFilter
    {
        public bool IsChunkAuthorizedForPrompt(Guid chunkOrg, Guid userOrg) => chunkOrg == userOrg;
    }
}

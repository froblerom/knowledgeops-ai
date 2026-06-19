using KnowledgeOps.Application.Chat;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Infrastructure.Chat;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.IntegrationTests;

public sealed class DemoGroundedAnswerGeneratorTests
{
    private static readonly IOptions<DemoGroundedAnswerGeneratorSettings> DefaultSettings =
        Options.Create(new DemoGroundedAnswerGeneratorSettings());

    private static AuthorizedChunkContext MakeChunk(
        string text = "Refunds are accepted within 14 days of purchase.",
        string? sectionLabel = null) =>
        new(
            ChunkId: Guid.NewGuid(),
            DocumentId: Guid.NewGuid(),
            OrganizationId: Guid.NewGuid(),
            ChunkText: text,
            ChunkIndex: 0,
            PageNumber: 1,
            SectionLabel: sectionLabel);

    [Fact]
    public async Task GenerateAsync_SingleChunk_ReturnsGroundedStateContainingChunkText()
    {
        var chunk = MakeChunk("The refund window is 14 days from delivery.");
        var generator = new DemoGroundedAnswerGenerator(DefaultSettings);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([chunk], "What is the refund window?"));

        Assert.Equal(AnswerState.Grounded, result.State);
        Assert.NotNull(result.AnswerText);
        Assert.Contains("The refund window is 14 days from delivery.", result.AnswerText);
    }

    [Fact]
    public async Task GenerateAsync_MultipleChunks_IncludesFirstTwoChunks()
    {
        var chunk1 = MakeChunk("First policy section content.");
        var chunk2 = MakeChunk("Second policy section content.");
        var chunk3 = MakeChunk("Third policy section content — should be excluded.");
        var generator = new DemoGroundedAnswerGenerator(DefaultSettings);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([chunk1, chunk2, chunk3], "What is the policy?"));

        Assert.Equal(AnswerState.Grounded, result.State);
        Assert.Contains("First policy section content.", result.AnswerText);
        Assert.Contains("Second policy section content.", result.AnswerText);
        Assert.DoesNotContain("Third policy section content", result.AnswerText);
    }

    [Fact]
    public async Task GenerateAsync_WithSectionLabel_IncludesSectionHeader()
    {
        var chunk = MakeChunk("Items must be returned in original condition.", "Return Conditions");
        var generator = new DemoGroundedAnswerGenerator(DefaultSettings);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([chunk], "What are the return conditions?"));

        Assert.Equal(AnswerState.Grounded, result.State);
        Assert.Contains("Return Conditions", result.AnswerText);
        Assert.Contains("Items must be returned in original condition.", result.AnswerText);
    }

    [Fact]
    public async Task GenerateAsync_WithoutSectionLabel_OmitsSectionHeader()
    {
        var chunk = MakeChunk("Policy text without a section label.", sectionLabel: null);
        var generator = new DemoGroundedAnswerGenerator(DefaultSettings);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([chunk], "What is the policy?"));

        Assert.Equal(AnswerState.Grounded, result.State);
        // No bold header should appear when section label is absent
        Assert.DoesNotContain("**", result.AnswerText!);
    }

    [Fact]
    public async Task GenerateAsync_EmptyChunks_ReturnsProviderFailed()
    {
        // The orchestration sufficiency gate prevents the generator from being called with zero
        // chunks, but if it somehow is, the generator must return ProviderFailed (not
        // InsufficientContext — that state is the orchestration's responsibility, not the generator's).
        var generator = new DemoGroundedAnswerGenerator(DefaultSettings);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([], "What is the policy?"));

        Assert.Equal(AnswerState.ProviderFailed, result.State);
        Assert.Null(result.AnswerText);
        Assert.Equal("NoUsableChunkText", result.SafeFailureCode);
    }

    [Fact]
    public async Task GenerateAsync_LongChunk_TruncatesGracefully()
    {
        var longText = new string('A', 1500); // exceeds MaxCharsPerChunk = 1000
        var chunk = MakeChunk(longText);
        var generator = new DemoGroundedAnswerGenerator(DefaultSettings);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([chunk], "What is the policy?"));

        Assert.Equal(AnswerState.Grounded, result.State);
        Assert.NotNull(result.AnswerText);
        Assert.EndsWith(
            "Please verify this information with the cited source and your supervisor before taking action.",
            result.AnswerText);
        Assert.Contains("...", result.AnswerText);
    }

    [Fact]
    public async Task GenerateAsync_DoesNotContainFakeOrPlaceholderWording()
    {
        var chunk = MakeChunk("Standard policy information.");
        var generator = new DemoGroundedAnswerGenerator(DefaultSettings);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([chunk], "What is the policy?"));

        Assert.NotNull(result.AnswerText);
        Assert.DoesNotContain("Fake", result.AnswerText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("placeholder", result.AnswerText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("test", result.AnswerText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authorized retrieved context(s)", result.AnswerText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_IsDeterministic_SameInputProducesSameOutput()
    {
        var chunk = MakeChunk("Policy says: all refunds processed within 5 business days.");
        var generator = new DemoGroundedAnswerGenerator(DefaultSettings);
        var request = new AnswerGenerationRequest([chunk], "How long for refund?");

        var first = await generator.GenerateAsync(request);
        var second = await generator.GenerateAsync(request);

        Assert.Equal(first.AnswerText, second.AnswerText);
    }

    [Fact]
    public async Task GenerateAsync_StartsWithProfessionalPreamble()
    {
        var chunk = MakeChunk("The damaged item procedure requires photo evidence.");
        var generator = new DemoGroundedAnswerGenerator(DefaultSettings);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([chunk], "What do I need for a damaged item claim?"));

        Assert.NotNull(result.AnswerText);
        Assert.StartsWith(
            "Based on the available knowledge base, here is the relevant guidance:",
            result.AnswerText);
    }

    [Fact]
    public async Task GenerateAsync_EndsWithVerificationDisclaimer()
    {
        var chunk = MakeChunk("Policy guidance text.");
        var generator = new DemoGroundedAnswerGenerator(DefaultSettings);

        var result = await generator.GenerateAsync(
            new AnswerGenerationRequest([chunk], "What is the policy?"));

        Assert.NotNull(result.AnswerText);
        Assert.EndsWith(
            "Please verify this information with the cited source and your supervisor before taking action.",
            result.AnswerText);
    }

    [Fact]
    public void GenerateAsync_ProviderNameAndModelAreDemo()
    {
        var generator = new DemoGroundedAnswerGenerator(DefaultSettings);

        Assert.Equal("Demo", generator.ProviderName);
        Assert.Equal("demo-extractive-v1", generator.DefaultModelName);
    }

    [Fact]
    public async Task GenerateAsync_SystemInstructionAndFormattedContextAreNotRequired()
    {
        var chunk = MakeChunk("Works without system instruction or formatted context.");
        var generator = new DemoGroundedAnswerGenerator(DefaultSettings);

        // No SystemInstruction, no FormattedContext — demo generator ignores them and uses ChunkText
        var result = await generator.GenerateAsync(new AnswerGenerationRequest(
            AuthorizedChunks: [chunk],
            UserQuestion: "Works?",
            SystemInstruction: null,
            FormattedContext: null));

        Assert.Equal(AnswerState.Grounded, result.State);
        Assert.NotNull(result.AnswerText);
    }
}

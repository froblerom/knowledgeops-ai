using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Domain.Tests.Chat;

public sealed class CitationTests
{
    private static readonly Guid InteractionId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid OrganizationId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid DocumentId = Guid.Parse("44444444-4444-4444-8444-444444444444");
    private static readonly Guid ChunkId = Guid.Parse("55555555-5555-4555-8555-555555555555");

    [Fact]
    public void Create_CapturesMetadataOnlyCitation()
    {
        var citation = Citation.Create(
            InteractionId,
            OrganizationId,
            DocumentId,
            ChunkId,
            2,
            "Refund Policy",
            7,
            "Escalations",
            0.87);

        Assert.NotEqual(Guid.Empty, citation.Id);
        Assert.Equal(InteractionId, citation.ChatInteractionId);
        Assert.Equal(OrganizationId, citation.OrganizationId);
        Assert.Equal(DocumentId, citation.DocumentId);
        Assert.Equal(ChunkId, citation.ChunkId);
        Assert.Equal(2, citation.Rank);
        Assert.Equal("Refund Policy", citation.DocumentTitle);
        Assert.Equal(7, citation.PageNumber);
        Assert.Equal("Escalations", citation.SectionLabel);
        Assert.Equal(0.87, citation.RelevanceScore!.Value, precision: 2);
        Assert.NotEqual(default, citation.CreatedAt);
    }

    [Fact]
    public void Create_WhenDocumentTitleIsNull_StoresEmptyTitle()
    {
        var citation = Citation.Create(
            InteractionId,
            OrganizationId,
            DocumentId,
            ChunkId,
            1,
            documentTitle: null!,
            pageNumber: null,
            sectionLabel: null,
            relevanceScore: 0.5);

        Assert.Equal(string.Empty, citation.DocumentTitle);
    }

    [Theory]
    [InlineData("chatInteractionId")]
    [InlineData("organizationId")]
    [InlineData("documentId")]
    [InlineData("chunkId")]
    public void Create_RequiresScopedIdentifiers(string missing)
    {
        var interactionId = missing == "chatInteractionId" ? Guid.Empty : InteractionId;
        var organizationId = missing == "organizationId" ? Guid.Empty : OrganizationId;
        var documentId = missing == "documentId" ? Guid.Empty : DocumentId;
        var chunkId = missing == "chunkId" ? Guid.Empty : ChunkId;

        Assert.Throws<ArgumentException>(() =>
            Citation.Create(
                interactionId,
                organizationId,
                documentId,
                chunkId,
                1,
                "Policy",
                pageNumber: null,
                sectionLabel: null,
                relevanceScore: 0.9));
    }

    [Fact]
    public void Create_RequiresPositiveRank()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Citation.Create(
                InteractionId,
                OrganizationId,
                DocumentId,
                ChunkId,
                0,
                "Policy",
                pageNumber: null,
                sectionLabel: null,
                relevanceScore: 0.9));
    }
}

using KnowledgeOps.Application.Chat.Citations;
using KnowledgeOps.Domain.Chat;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Chat.Citations;

public sealed class CitationMapperTests
{
    private static readonly Guid InteractionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrganizationId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid DocumentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ChunkId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task CitationMapper_UnavailableScore_RemainsNull()
    {
        var titleReader = new FakeDocumentTitleReader(new Dictionary<Guid, string> { [DocumentId] = "Policy Doc" });
        var mapper = new CitationMapper(titleReader, NullLogger<CitationMapper>.Instance);

        var sources = new[]
        {
            new CitationMappingSource(DocumentId, ChunkId, Rank: 1, RelevanceScore: null, PageNumber: null, SectionLabel: null),
        };

        var citations = await mapper.MapAsync(InteractionId, OrganizationId, sources);

        var citation = Assert.Single(citations);
        Assert.Null(citation.RelevanceScore);
    }

    [Fact]
    public async Task CitationMapper_AvailableScore_IsPreserved()
    {
        var titleReader = new FakeDocumentTitleReader(new Dictionary<Guid, string> { [DocumentId] = "Policy Doc" });
        var mapper = new CitationMapper(titleReader, NullLogger<CitationMapper>.Instance);

        var sources = new[]
        {
            new CitationMappingSource(DocumentId, ChunkId, Rank: 1, RelevanceScore: 0.87, PageNumber: null, SectionLabel: null),
        };

        var citations = await mapper.MapAsync(InteractionId, OrganizationId, sources);

        var citation = Assert.Single(citations);
        Assert.NotNull(citation.RelevanceScore);
        Assert.Equal(0.87, citation.RelevanceScore!.Value, precision: 9);
    }

    [Fact]
    public async Task CitationMapper_MapsSources_WithDocumentAndChunkReferences()
    {
        var titleReader = new FakeDocumentTitleReader(new Dictionary<Guid, string> { [DocumentId] = "Policy Doc" });
        var mapper = new CitationMapper(titleReader, NullLogger<CitationMapper>.Instance);

        var sources = new[]
        {
            new CitationMappingSource(DocumentId, ChunkId, Rank: 1, RelevanceScore: 0.9, PageNumber: 3, SectionLabel: "Intro"),
        };

        var citations = await mapper.MapAsync(InteractionId, OrganizationId, sources);

        var citation = Assert.Single(citations);
        Assert.Equal(DocumentId, citation.DocumentId);
        Assert.Equal(ChunkId, citation.ChunkId);
        Assert.Equal(1, citation.Rank);
        Assert.Equal("Policy Doc", citation.DocumentTitle);
        Assert.Equal(3, citation.PageNumber);
        Assert.Equal("Intro", citation.SectionLabel);
    }

    [Fact]
    public async Task CitationMapper_DoesNotRequireOrStoreChunkText()
    {
        var titleReader = new FakeDocumentTitleReader(new Dictionary<Guid, string> { [DocumentId] = "Doc" });
        var mapper = new CitationMapper(titleReader, NullLogger<CitationMapper>.Instance);

        var sources = new[]
        {
            new CitationMappingSource(DocumentId, ChunkId, Rank: 1, RelevanceScore: 0.5, PageNumber: null, SectionLabel: null),
        };

        var citations = await mapper.MapAsync(InteractionId, OrganizationId, sources);

        var citation = Assert.Single(citations);
        // Citation entity has no chunk text field
        Assert.Equal(DocumentId, citation.DocumentId);
        Assert.Equal(ChunkId, citation.ChunkId);
    }

    [Fact]
    public async Task CitationMapper_UsesFallbackTitle_WhenDocumentNotFound()
    {
        var titleReader = new FakeDocumentTitleReader(new Dictionary<Guid, string>());
        var mapper = new CitationMapper(titleReader, NullLogger<CitationMapper>.Instance);

        var sources = new[]
        {
            new CitationMappingSource(DocumentId, ChunkId, Rank: 1, RelevanceScore: null, PageNumber: null, SectionLabel: null),
        };

        var citations = await mapper.MapAsync(InteractionId, OrganizationId, sources);

        var citation = Assert.Single(citations);
        Assert.Equal("Unknown Document", citation.DocumentTitle);
    }

    [Fact]
    public async Task CitationMapper_ReturnsEmpty_ForEmptySources()
    {
        var titleReader = new FakeDocumentTitleReader(new Dictionary<Guid, string>());
        var mapper = new CitationMapper(titleReader, NullLogger<CitationMapper>.Instance);

        var citations = await mapper.MapAsync(InteractionId, OrganizationId, []);

        Assert.Empty(citations);
    }

    // ─── Fakes ───────────────────────────────────────────────────────────────

    private sealed class FakeDocumentTitleReader(
        IReadOnlyDictionary<Guid, string> titles) : IDocumentTitleReader
    {
        public Task<IReadOnlyDictionary<Guid, string>> GetTitlesAsync(
            IReadOnlyList<Guid> documentIds,
            Guid organizationId,
            CancellationToken ct = default) =>
            Task.FromResult(titles);
    }
}

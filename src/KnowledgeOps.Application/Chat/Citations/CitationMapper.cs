using KnowledgeOps.Domain.Chat;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Chat.Citations;

internal sealed class CitationMapper(
    IDocumentTitleReader titleReader,
    ILogger<CitationMapper> logger) : ICitationMapper
{
    private const string FallbackDocumentTitle = "Unknown Document";

    public async Task<IReadOnlyList<Citation>> MapAsync(
        Guid chatInteractionId,
        Guid organizationId,
        IReadOnlyList<CitationMappingSource> sources,
        CancellationToken ct = default)
    {
        if (sources.Count == 0)
            return [];

        var documentIds = sources.Select(s => s.DocumentId).Distinct().ToArray();

        IReadOnlyDictionary<Guid, string> titles;
        try
        {
            titles = await titleReader.GetTitlesAsync(documentIds, organizationId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                "Citation document title lookup failed. InteractionId={InteractionId} OrganizationId={OrganizationId}",
                chatInteractionId,
                organizationId);
            titles = new Dictionary<Guid, string>();
        }

        var citations = new List<Citation>(sources.Count);
        foreach (var source in sources)
        {
            var title = titles.TryGetValue(source.DocumentId, out var t) ? t : FallbackDocumentTitle;
            citations.Add(Citation.Create(
                chatInteractionId,
                organizationId,
                source.DocumentId,
                source.ChunkId,
                source.Rank,
                title,
                source.PageNumber,
                source.SectionLabel,
                source.RelevanceScore));
        }

        return citations;
    }
}

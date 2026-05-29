using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Application.Chat.Citations;

public interface ICitationMapper
{
    Task<IReadOnlyList<Citation>> MapAsync(
        Guid chatInteractionId,
        Guid organizationId,
        IReadOnlyList<CitationMappingSource> sources,
        CancellationToken ct = default);
}

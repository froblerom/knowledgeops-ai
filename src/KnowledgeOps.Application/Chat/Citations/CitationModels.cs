namespace KnowledgeOps.Application.Chat.Citations;

public sealed record CitationMappingSource(
    Guid DocumentId,
    Guid ChunkId,
    int Rank,
    double? RelevanceScore,
    int? PageNumber,
    string? SectionLabel);

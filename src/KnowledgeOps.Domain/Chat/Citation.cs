namespace KnowledgeOps.Domain.Chat;

public sealed class Citation
{
    public const int DocumentTitleMaxLength = 300;
    public const int SectionLabelMaxLength = 300;

    public Guid Id { get; init; }
    public Guid ChatInteractionId { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid DocumentId { get; init; }
    public Guid ChunkId { get; init; }
    public int Rank { get; init; }
    public string DocumentTitle { get; init; } = string.Empty;
    public int? PageNumber { get; init; }
    public string? SectionLabel { get; init; }
    public double? RelevanceScore { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public static Citation Create(
        Guid chatInteractionId,
        Guid organizationId,
        Guid documentId,
        Guid chunkId,
        int rank,
        string documentTitle,
        int? pageNumber,
        string? sectionLabel,
        double? relevanceScore)
    {
        if (chatInteractionId == Guid.Empty)
            throw new ArgumentException("ChatInteractionId must not be empty.", nameof(chatInteractionId));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId must not be empty.", nameof(organizationId));
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId must not be empty.", nameof(documentId));
        if (chunkId == Guid.Empty)
            throw new ArgumentException("ChunkId must not be empty.", nameof(chunkId));
        if (rank <= 0)
            throw new ArgumentOutOfRangeException(nameof(rank), "Rank must be positive.");

        return new Citation
        {
            Id = Guid.NewGuid(),
            ChatInteractionId = chatInteractionId,
            OrganizationId = organizationId,
            DocumentId = documentId,
            ChunkId = chunkId,
            Rank = rank,
            DocumentTitle = documentTitle ?? string.Empty,
            PageNumber = pageNumber,
            SectionLabel = sectionLabel,
            RelevanceScore = relevanceScore, // null when score is unavailable
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}

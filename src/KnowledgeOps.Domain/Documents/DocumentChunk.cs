namespace KnowledgeOps.Domain.Documents;

public sealed class DocumentChunk
{
    public Guid Id { get; init; }
    public Guid DocumentId { get; init; }
    public Guid OrganizationId { get; init; }
    public int ChunkIndex { get; init; }
    public string Text { get; init; } = string.Empty;
    public int? PageNumber { get; init; }
    public string? SectionLabel { get; init; }
    public int? CharacterLength { get; init; }
    public int? TokenEstimate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? DeletedAt { get; set; }
}

namespace KnowledgeOps.Domain.Documents;

public sealed class ChunkEmbedding
{
    public Guid Id { get; init; }
    public Guid ChunkId { get; init; }
    public Guid OrganizationId { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public string? VectorData { get; init; }
    public int? VectorDimensions { get; init; }
    public EmbeddingStatus Status { get; init; }
    public string? FailureReason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

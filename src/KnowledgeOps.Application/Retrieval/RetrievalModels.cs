namespace KnowledgeOps.Application.Retrieval;

public sealed record VectorIndexRequest(Guid OrganizationId, Guid? DocumentId = null);

public sealed record VectorIndexResult(
    int EligibleEmbeddingCount,
    int IndexedCount,
    int FailedCount,
    int SkippedCount,
    RetrievalProviderMetadata ProviderMetadata);

public sealed record SemanticQueryRequest(
    Guid OrganizationId,
    IReadOnlyList<float> QueryVector,
    int TopK,
    double? MinimumScore = null);

public sealed record SemanticQueryResult(
    IReadOnlyList<RetrievedChunkCandidate> Candidates,
    string ScoreMethod,
    int RequestedTopK,
    int EffectiveTopK,
    int TotalEligibleScanned,
    int ExcludedMalformedVectorCount,
    int ExcludedDimensionMismatchCount,
    int ExcludedZeroNormVectorCount,
    RetrievalProviderMetadata ProviderMetadata);

public sealed record RetrievedChunkCandidate(
    Guid OrganizationId,
    Guid DocumentId,
    Guid ChunkId,
    Guid ChunkEmbeddingId,
    RetrievalScore RetrievalScore,
    string ScoreMethod,
    string ProviderName,
    string ModelName,
    int? ChunkIndex = null,
    int? PageNumber = null,
    string? SectionLabel = null);

public sealed record RetrievalScore(double Value, string Method);

public sealed record RetrievalProviderMetadata(
    string ProviderName,
    string AdapterName,
    string StorageKind);

public sealed record RetrievalStorageHealthResult(
    bool IsHealthy,
    string ProviderName,
    int IndexedEmbeddingCount,
    int FailedIndexCount,
    string? DegradedReason = null);

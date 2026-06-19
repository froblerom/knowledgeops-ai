using System.Diagnostics;
using System.Text.Json;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Application.Retrieval;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.Infrastructure.Retrieval;

internal sealed class LocalVectorStore(
    KnowledgeOpsDbContext dbContext,
    IOptions<RetrievalSettings> settings,
    IAuditEventWriter auditEventWriter,
    ILogger<LocalVectorStore> logger) : IRetrievalIndex, ISemanticSearchProvider
{
    public const string ProviderName = "LocalSqlVectorStore";
    private const string AdapterName = "LocalVectorStore";
    private const string StorageKind = "SqlJsonVectorData";
    private const string ScoreMethod = "CosineSimilarity";
    private const string InvalidVectorFailureReason = "Embedding vector data was invalid.";

    private static readonly RetrievalProviderMetadata Metadata = new(
        ProviderName,
        AdapterName,
        StorageKind);

    public async Task<VectorIndexResult> IndexAsync(
        VectorIndexRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        // IndexAsync does not require ProcessingStatus == Processed or IsRetrievalEnabled:
        // the indexing step runs while the document is still in Processing status,
        // and IsRetrievalEnabled defaults to false until the user explicitly enables it.
        // SearchAsync enforces both conditions — indexing only prepares the embedding metadata.
        var eligibleEmbeddings = await (
            from embedding in dbContext.ChunkEmbeddings
            join chunk in dbContext.DocumentChunks on embedding.ChunkId equals chunk.Id
            join document in dbContext.Documents on chunk.DocumentId equals document.Id
            where embedding.OrganizationId == request.OrganizationId
                && embedding.Status == EmbeddingStatus.Ready
                && embedding.IndexStatus == null
                && chunk.OrganizationId == request.OrganizationId
                && chunk.DeletedAt == null
                && document.OrganizationId == request.OrganizationId
                && document.DeletedAt == null
                && (request.DocumentId == null || document.Id == request.DocumentId.Value)
            select embedding)
            .ToListAsync(cancellationToken);

        var indexedCount = 0;
        var failedCount = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var embedding in eligibleEmbeddings)
        {
            if (TryParseVector(embedding.VectorData, out var vector)
                && IsVectorUsable(vector)
                && DimensionMatches(embedding.VectorDimensions, vector))
            {
                embedding.IndexStatus = EmbeddingIndexStatus.Indexed;
                embedding.IndexedAt = now;
                embedding.IndexFailureReason = null;
                indexedCount++;
            }
            else
            {
                embedding.IndexStatus = EmbeddingIndexStatus.Failed;
                embedding.IndexedAt = null;
                embedding.IndexFailureReason = InvalidVectorFailureReason;
                failedCount++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        stopwatch.Stop();

        var eventType = failedCount == 0
            ? AuditEventTypes.VectorIndexingSucceeded
            : AuditEventTypes.VectorIndexingFailed;
        var severity = failedCount == 0 ? AuditSeverity.Info : AuditSeverity.Warning;

        await TryWriteAuditAsync(
            eventType,
            $"Vector indexing complete. Indexed={indexedCount} Failed={failedCount}",
            severity,
            request.OrganizationId,
            entityType: request.DocumentId.HasValue ? "Document" : null,
            entityId: request.DocumentId,
            cancellationToken);

        logger.LogInformation(
            "Vector indexing completed. ProviderName={ProviderName} OrganizationId={OrganizationId} Indexed={Indexed} Failed={Failed} DurationMs={DurationMs}",
            ProviderName,
            request.OrganizationId,
            indexedCount,
            failedCount,
            stopwatch.ElapsedMilliseconds);

        return new VectorIndexResult(
            EligibleEmbeddingCount: eligibleEmbeddings.Count,
            IndexedCount: indexedCount,
            FailedCount: failedCount,
            SkippedCount: 0,
            ProviderMetadata: Metadata);
    }

    public async Task<SemanticQueryResult> SearchAsync(
        SemanticQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestedTopK = request.TopK;
        var effectiveTopK = settings.Value.NormalizeTopK(requestedTopK);
        var stopwatch = Stopwatch.StartNew();

        if (!IsVectorUsable(request.QueryVector))
        {
            return new SemanticQueryResult(
                [],
                ScoreMethod,
                requestedTopK,
                effectiveTopK,
                TotalEligibleScanned: 0,
                ExcludedMalformedVectorCount: 0,
                ExcludedDimensionMismatchCount: 0,
                ExcludedZeroNormVectorCount: 0,
                ProviderMetadata: Metadata);
        }

        try
        {
            var rows = await (
                from embedding in dbContext.ChunkEmbeddings.AsNoTracking()
                join chunk in dbContext.DocumentChunks.AsNoTracking() on embedding.ChunkId equals chunk.Id
                join document in dbContext.Documents.AsNoTracking() on chunk.DocumentId equals document.Id
                where embedding.OrganizationId == request.OrganizationId
                    && embedding.Status == EmbeddingStatus.Ready
                    && embedding.IndexStatus == EmbeddingIndexStatus.Indexed
                    && embedding.VectorData != null
                    && chunk.OrganizationId == request.OrganizationId
                    && chunk.DeletedAt == null
                    && document.OrganizationId == request.OrganizationId
                    && document.ProcessingStatus == DocumentProcessingStatus.Processed
                    && document.IsRetrievalEnabled
                    && document.DeletedAt == null
                select new CandidateRow(
                    embedding.Id,
                    embedding.OrganizationId,
                    embedding.ChunkId,
                    document.Id,
                    embedding.ProviderName,
                    embedding.ModelName,
                    embedding.VectorData!,
                    chunk.ChunkIndex,
                    chunk.PageNumber,
                    chunk.SectionLabel))
                .ToListAsync(cancellationToken);

            var candidates = new List<RetrievedChunkCandidate>(rows.Count);
            var malformedCount = 0;
            var dimensionMismatchCount = 0;
            var zeroNormCount = 0;

            foreach (var row in rows)
            {
                if (!TryParseVector(row.VectorData, out var vector))
                {
                    malformedCount++;
                    continue;
                }

                if (vector.Length != request.QueryVector.Count)
                {
                    dimensionMismatchCount++;
                    continue;
                }

                if (!TryCosineSimilarity(request.QueryVector, vector, out var score))
                {
                    zeroNormCount++;
                    continue;
                }

                if (request.MinimumScore.HasValue && score < request.MinimumScore.Value)
                    continue;

                var retrievalScore = new RetrievalScore(score, ScoreMethod);
                candidates.Add(new RetrievedChunkCandidate(
                    row.OrganizationId,
                    row.DocumentId,
                    row.ChunkId,
                    row.ChunkEmbeddingId,
                    retrievalScore,
                    ScoreMethod,
                    row.ProviderName,
                    row.ModelName,
                    row.ChunkIndex,
                    row.PageNumber,
                    row.SectionLabel));
            }

            var ranked = candidates
                .OrderByDescending(candidate => candidate.RetrievalScore.Value)
                .ThenBy(candidate => candidate.ChunkIndex ?? int.MaxValue)
                .ThenBy(candidate => candidate.ChunkId)
                .Take(effectiveTopK)
                .ToArray();

            stopwatch.Stop();

            await TryWriteAuditAsync(
                AuditEventTypes.SemanticQueryCompleted,
                $"Semantic query completed. ResultCount={ranked.Length} TopK={effectiveTopK}",
                AuditSeverity.Info,
                request.OrganizationId,
                entityType: null,
                entityId: null,
                cancellationToken);

            if (malformedCount > 0)
            {
                await TryWriteAuditAsync(
                    AuditEventTypes.MalformedVectorExcluded,
                    $"Malformed vectors excluded. Count={malformedCount}",
                    AuditSeverity.Warning,
                    request.OrganizationId,
                    entityType: null,
                    entityId: null,
                    cancellationToken);
            }

            logger.LogInformation(
                "Semantic query completed. ProviderName={ProviderName} OrganizationId={OrganizationId} ResultCount={ResultCount} TopK={TopK} DurationMs={DurationMs} Malformed={Malformed} DimensionMismatch={DimensionMismatch} ZeroNorm={ZeroNorm}",
                ProviderName,
                request.OrganizationId,
                ranked.Length,
                effectiveTopK,
                stopwatch.ElapsedMilliseconds,
                malformedCount,
                dimensionMismatchCount,
                zeroNormCount);

            return new SemanticQueryResult(
                ranked,
                ScoreMethod,
                requestedTopK,
                effectiveTopK,
                rows.Count,
                malformedCount,
                dimensionMismatchCount,
                zeroNormCount,
                Metadata);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            await TryWriteAuditAsync(
                AuditEventTypes.SemanticQueryFailed,
                "Semantic query failed.",
                AuditSeverity.Error,
                request.OrganizationId,
                entityType: null,
                entityId: null,
                cancellationToken);

            logger.LogError(
                "Semantic query failed. ProviderName={ProviderName} OrganizationId={OrganizationId} FailureCategory={FailureCategory} DurationMs={DurationMs}",
                ProviderName,
                request.OrganizationId,
                ProviderFailureCategory.Retrieval,
                stopwatch.ElapsedMilliseconds);

            throw new InvalidOperationException("Semantic retrieval storage query failed.");
        }
    }

    internal static bool TryParseVector(string? vectorData, out float[] vector)
    {
        vector = [];

        if (string.IsNullOrWhiteSpace(vectorData))
            return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<float[]>(vectorData);
            if (parsed is null || parsed.Length == 0)
                return false;

            if (parsed.Any(value => !float.IsFinite(value)))
                return false;

            vector = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    internal static bool TryCosineSimilarity(
        IReadOnlyList<float> left,
        IReadOnlyList<float> right,
        out double similarity)
    {
        similarity = 0;

        if (left.Count == 0 || left.Count != right.Count)
            return false;

        double dot = 0;
        double leftNorm = 0;
        double rightNorm = 0;

        for (var i = 0; i < left.Count; i++)
        {
            var leftValue = left[i];
            var rightValue = right[i];

            if (!float.IsFinite(leftValue) || !float.IsFinite(rightValue))
                return false;

            dot += leftValue * rightValue;
            leftNorm += leftValue * leftValue;
            rightNorm += rightValue * rightValue;
        }

        if (leftNorm <= 0 || rightNorm <= 0)
            return false;

        similarity = dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
        return double.IsFinite(similarity);
    }

    private static bool IsVectorUsable(IReadOnlyList<float> vector) =>
        TryCosineSimilarity(vector, vector, out _);

    private static bool DimensionMatches(int? expectedDimensions, IReadOnlyList<float> vector) =>
        expectedDimensions is null || expectedDimensions.Value == vector.Count;

    private async Task TryWriteAuditAsync(
        string eventType,
        string message,
        AuditSeverity severity,
        Guid organizationId,
        string? entityType,
        Guid? entityId,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditEventWriter.WriteAsync(
                new AuditEvent(
                    eventType,
                    message,
                    severity,
                    CorrelationIdPolicy.AcceptOrCreate(null),
                    organizationId,
                    UserId: null,
                    entityType,
                    entityId),
                cancellationToken);
        }
        catch
        {
            logger.LogWarning(
                "Retrieval audit write failed. EventType={EventType} OrganizationId={OrganizationId}",
                eventType,
                organizationId);
        }
    }

    private sealed record CandidateRow(
        Guid ChunkEmbeddingId,
        Guid OrganizationId,
        Guid ChunkId,
        Guid DocumentId,
        string ProviderName,
        string ModelName,
        string VectorData,
        int ChunkIndex,
        int? PageNumber,
        string? SectionLabel);
}

using System.Security.Cryptography;
using System.Text;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Embeddings;
using KnowledgeOps.Application.Observability;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Retrieval;

internal sealed class EligibleSemanticRetrievalService(
    ICurrentUser currentUser,
    IUserAccessStateReader accessStateReader,
    IPermissionService permissionService,
    IEmbeddingProvider embeddingProvider,
    ISemanticSearchProvider semanticSearchProvider,
    IRetrievalEligibilityRepository eligibilityRepository,
    IAuditEventWriter auditEventWriter,
    ICorrelationContext correlationContext,
    ILogger<EligibleSemanticRetrievalService> logger) : IEligibleSemanticRetrievalService
{
    public async Task<EligibleSemanticRetrievalResult> RetrieveAsync(
        EligibleSemanticRetrievalRequest request,
        CancellationToken cancellationToken = default)
    {
        var retrievalQueryId = Guid.NewGuid();
        var trimmedQuery = request.QueryText?.Trim() ?? string.Empty;
        var queryHash = ComputeQueryHash(trimmedQuery);

        if (!currentUser.IsAuthenticated)
        {
            return Failure(
                retrievalQueryId,
                queryHash,
                request.TopK,
                "Unauthenticated",
                "Retrieval request could not be authorized.");
        }

        UserAccessState? activeState;
        try
        {
            activeState = await accessStateReader.FindActiveByIdAsync(
                currentUser.UserId,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                "Retrieval authorization state lookup failed. CorrelationId={CorrelationId} UserId={UserId}",
                correlationContext.CorrelationId,
                currentUser.UserId);

            return Failure(
                retrievalQueryId,
                queryHash,
                request.TopK,
                "AuthorizationStateUnavailable",
                "Retrieval request could not be authorized.");
        }

        if (activeState is null)
        {
            return Failure(
                retrievalQueryId,
                queryHash,
                request.TopK,
                "UserInactive",
                "Retrieval request could not be authorized.");
        }

        if (!permissionService.HasPermission(activeState, KnowledgeOpsPermissions.Chat.AskQuestion))
        {
            return Failure(
                retrievalQueryId,
                queryHash,
                request.TopK,
                "PermissionDenied",
                "Retrieval request could not be authorized.");
        }

        if (activeState.OrganizationId == Guid.Empty)
        {
            return Failure(
                retrievalQueryId,
                queryHash,
                request.TopK,
                "InvalidOrganization",
                "Retrieval request could not be authorized.");
        }

        if (string.IsNullOrWhiteSpace(trimmedQuery))
        {
            return Failure(
                retrievalQueryId,
                queryHash,
                request.TopK,
                "InvalidQuery",
                "Retrieval query text is required.");
        }

        EmbeddingResponse embedding;
        try
        {
            embedding = await embeddingProvider.GenerateAsync(
                new EmbeddingRequest(
                    trimmedQuery,
                    embeddingProvider.DefaultModelName,
                    embeddingProvider.DefaultDimensions),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await AuditAsync(
                AuditEventTypes.QueryEmbeddingFailed,
                "Query embedding generation failed.",
                AuditSeverity.Error,
                activeState.OrganizationId,
                activeState.UserId,
                retrievalQueryId,
                cancellationToken);

            logger.LogWarning(
                "Query embedding generation failed. CorrelationId={CorrelationId} RetrievalQueryId={RetrievalQueryId} OrganizationId={OrganizationId}",
                correlationContext.CorrelationId,
                retrievalQueryId,
                activeState.OrganizationId);

            return Failure(
                retrievalQueryId,
                queryHash,
                request.TopK,
                "QueryEmbeddingFailed",
                "Query embedding generation failed.");
        }

        if (!IsVectorUsable(embedding.Vector))
        {
            await AuditAsync(
                AuditEventTypes.QueryEmbeddingFailed,
                "Query embedding generation returned an unusable vector.",
                AuditSeverity.Warning,
                activeState.OrganizationId,
                activeState.UserId,
                retrievalQueryId,
                cancellationToken);

            return Failure(
                retrievalQueryId,
                queryHash,
                request.TopK,
                "QueryEmbeddingFailed",
                "Query embedding generation failed.");
        }

        SemanticQueryResult semanticResult;
        try
        {
            semanticResult = await semanticSearchProvider.SearchAsync(
                new SemanticQueryRequest(
                    activeState.OrganizationId,
                    embedding.Vector,
                    request.TopK,
                    request.MinimumScore),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await AuditAsync(
                AuditEventTypes.EligibleSemanticRetrievalFailed,
                "Eligible semantic retrieval failed during semantic search.",
                AuditSeverity.Error,
                activeState.OrganizationId,
                activeState.UserId,
                retrievalQueryId,
                cancellationToken);

            logger.LogWarning(
                "Eligible semantic retrieval failed during semantic search. CorrelationId={CorrelationId} RetrievalQueryId={RetrievalQueryId} OrganizationId={OrganizationId}",
                correlationContext.CorrelationId,
                retrievalQueryId,
                activeState.OrganizationId);

            return Failure(
                retrievalQueryId,
                queryHash,
                request.TopK,
                "SemanticSearchFailed",
                "Semantic retrieval failed.");
        }

        var providerCandidates = semanticResult.Candidates;
        var organizationCandidates = providerCandidates
            .Where(candidate => candidate.OrganizationId == activeState.OrganizationId)
            .ToArray();
        var crossOrganizationExcludedCount = providerCandidates.Count - organizationCandidates.Length;

        if (crossOrganizationExcludedCount > 0)
        {
            await AuditAsync(
                AuditEventTypes.StaleRetrievalCandidateExcluded,
                $"Retrieval candidates excluded during organization validation. Count={crossOrganizationExcludedCount}",
                AuditSeverity.Warning,
                activeState.OrganizationId,
                activeState.UserId,
                retrievalQueryId,
                cancellationToken);
        }

        IReadOnlyList<RetrievalEligibleCandidateIdentity> revalidated;
        try
        {
            revalidated = await eligibilityRepository.RevalidateAsync(
                activeState.OrganizationId,
                organizationCandidates
                    .Select(candidate => new RetrievalCandidateIdentity(
                        candidate.OrganizationId,
                        candidate.DocumentId,
                        candidate.ChunkId,
                        candidate.ChunkEmbeddingId))
                    .ToArray(),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await AuditAsync(
                AuditEventTypes.EligibleSemanticRetrievalFailed,
                "Eligible semantic retrieval failed during eligibility revalidation.",
                AuditSeverity.Error,
                activeState.OrganizationId,
                activeState.UserId,
                retrievalQueryId,
                cancellationToken);

            logger.LogWarning(
                "Eligible semantic retrieval failed during eligibility revalidation. CorrelationId={CorrelationId} RetrievalQueryId={RetrievalQueryId} OrganizationId={OrganizationId}",
                correlationContext.CorrelationId,
                retrievalQueryId,
                activeState.OrganizationId);

            return Failure(
                retrievalQueryId,
                queryHash,
                request.TopK,
                "EligibilityRevalidationFailed",
                "Retrieval eligibility revalidation failed.",
                semanticResult.ProviderMetadata);
        }

        var eligibleSet = revalidated
            .Select(IdentityKey.From)
            .ToHashSet();

        var finalLimit = semanticResult.EffectiveTopK > 0
            ? semanticResult.EffectiveTopK
            : request.TopK > 0 ? request.TopK : int.MaxValue;
        var rank = 0;
        var finalCandidates = organizationCandidates
            .Where(candidate => eligibleSet.Contains(IdentityKey.From(candidate)))
            .Take(finalLimit)
            .Select(candidate => new EligibleSemanticRetrievalCandidate(
                Rank: ++rank,
                OrganizationId: candidate.OrganizationId,
                DocumentId: candidate.DocumentId,
                ChunkId: candidate.ChunkId,
                ChunkEmbeddingId: candidate.ChunkEmbeddingId,
                RetrievalScore: candidate.RetrievalScore,
                ScoreMethod: candidate.ScoreMethod,
                ProviderName: candidate.ProviderName,
                ModelName: candidate.ModelName,
                ChunkIndex: candidate.ChunkIndex,
                PageNumber: candidate.PageNumber,
                SectionLabel: candidate.SectionLabel))
            .ToArray();

        var staleExcludedCount = organizationCandidates.Length - finalCandidates.Length;
        if (staleExcludedCount > 0)
        {
            await AuditAsync(
                AuditEventTypes.StaleRetrievalCandidateExcluded,
                $"Retrieval candidates excluded during eligibility revalidation. Count={staleExcludedCount}",
                AuditSeverity.Warning,
                activeState.OrganizationId,
                activeState.UserId,
                retrievalQueryId,
                cancellationToken);
        }

        await AuditAsync(
            AuditEventTypes.EligibleSemanticRetrievalCompleted,
            $"Eligible semantic retrieval completed. ReturnedCount={finalCandidates.Length} RequestedTopK={request.TopK}",
            AuditSeverity.Info,
            activeState.OrganizationId,
            activeState.UserId,
            retrievalQueryId,
            cancellationToken);

        var isInsufficient = finalCandidates.Length == 0;
        if (isInsufficient)
        {
            await AuditAsync(
                AuditEventTypes.RetrievalInsufficientResults,
                $"Retrieval returned insufficient results. ReturnedCount=0 RequestedTopK={request.TopK}",
                AuditSeverity.Info,
                activeState.OrganizationId,
                activeState.UserId,
                retrievalQueryId,
                cancellationToken);
        }

        return new EligibleSemanticRetrievalResult(
            retrievalQueryId,
            queryHash,
            isInsufficient,
            finalCandidates,
            request.TopK,
            finalCandidates.Length,
            semanticResult.ProviderMetadata);
    }

    private static EligibleSemanticRetrievalResult Failure(
        Guid retrievalQueryId,
        string queryHash,
        int requestedTopK,
        string failureCode,
        string failureReason,
        RetrievalProviderMetadata? providerMetadata = null) =>
        new(
            retrievalQueryId,
            queryHash,
            IsInsufficientResult: false,
            Candidates: [],
            RequestedTopK: requestedTopK,
            ReturnedCount: 0,
            ProviderMetadata: providerMetadata,
            FailureCode: failureCode,
            FailureReason: failureReason);

    private static string ComputeQueryHash(string trimmedQuery)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(trimmedQuery));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool IsVectorUsable(IReadOnlyList<float> vector) =>
        vector.Count > 0
        && vector.All(float.IsFinite)
        && vector.Any(value => value != 0);

    private async Task AuditAsync(
        string eventType,
        string message,
        AuditSeverity severity,
        Guid organizationId,
        Guid userId,
        Guid retrievalQueryId,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditEventWriter.WriteAsync(
                new AuditEvent(
                    eventType,
                    message,
                    severity,
                    correlationContext.CorrelationId,
                    organizationId,
                    userId,
                    "RetrievalQuery",
                    retrievalQueryId),
                cancellationToken);
        }
        catch
        {
            logger.LogWarning(
                "Retrieval audit write failed. EventType={EventType} CorrelationId={CorrelationId}",
                eventType,
                correlationContext.CorrelationId);
        }
    }

    private sealed record IdentityKey(Guid DocumentId, Guid ChunkId, Guid ChunkEmbeddingId)
    {
        public static IdentityKey From(RetrievalEligibleCandidateIdentity identity) =>
            new(identity.DocumentId, identity.ChunkId, identity.ChunkEmbeddingId);

        public static IdentityKey From(RetrievedChunkCandidate candidate) =>
            new(candidate.DocumentId, candidate.ChunkId, candidate.ChunkEmbeddingId);
    }
}

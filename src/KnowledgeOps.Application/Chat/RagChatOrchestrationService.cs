using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Authorization.Hooks;
using KnowledgeOps.Application.Chat.Citations;
using KnowledgeOps.Application.Chat.Prompting;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Application.Retrieval;
using KnowledgeOps.Domain.Chat;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Chat;

internal sealed class RagChatOrchestrationService(
    ICurrentUser currentUser,
    IUserAccessStateReader accessStateReader,
    IPermissionService permissionService,
    IEligibleSemanticRetrievalService retrievalService,
    IAiAnswerGenerator answerGenerator,
    IChatSessionRepository sessionRepository,
    IChatInteractionRepository interactionRepository,
    IChunkTextReader chunkTextReader,
    IGroundedPromptBuilder promptBuilder,
    IContextSufficiencyPolicy sufficiencyPolicy,
    ICitationMapper citationMapper,
    ICitationRepository citationRepository,
    ICitationAuthorizationFilter citationAuthorizationFilter,
    IAuditEventWriter auditWriter,
    ICorrelationContext correlationContext,
    ILogger<RagChatOrchestrationService> logger) : IRagChatOrchestrationService
{
    private const string InsufficientContextFallbackText =
        "I could not find sufficient information in the knowledge base to answer your question.";
    public async Task<AskQuestionResponse> AskAsync(
        AskQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Validate authenticated user
        if (!currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        // Step 2: Load UserAccessState
        UserAccessState? activeState;
        try
        {
            activeState = await accessStateReader.FindActiveByIdAsync(currentUser.UserId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                "Chat authorization state lookup failed. CorrelationId={CorrelationId} UserId={UserId}",
                correlationContext.CorrelationId,
                currentUser.UserId);
            throw new UnauthorizedAccessException("Authorization state is unavailable.");
        }

        if (activeState is null)
            throw new UnauthorizedAccessException("User is not active.");

        // Step 3: Check Chat.AskQuestion permission
        if (!permissionService.HasPermission(activeState, KnowledgeOpsPermissions.Chat.AskQuestion))
            throw new UnauthorizedAccessException("Permission denied.");

        // Step 4: Validate organization scope
        if (activeState.OrganizationId == Guid.Empty)
            throw new InvalidOperationException("Organization scope is not set.");

        // Step 5: Validate question text
        var trimmedQuestion = request.QuestionText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedQuestion))
            throw new ArgumentException("Question text is required.", nameof(request));

        var questionHash = ComputeHash(trimmedQuestion);
        var correlationId = correlationContext.CorrelationId;

        // Step 6: Create or load ChatSession
        ChatSession session;
        var isNewSession = false;
        if (request.ChatSessionId.HasValue)
        {
            var existing = await sessionRepository.FindByIdAsync(request.ChatSessionId.Value, cancellationToken);
            if (existing is null || !existing.IsOwnedBy(activeState.UserId, activeState.OrganizationId))
                throw new UnauthorizedAccessException("Chat session not found or not accessible.");
            session = existing;
        }
        else
        {
            session = ChatSession.Create(activeState.OrganizationId, activeState.UserId, title: null);
            isNewSession = true;
        }

        // Step 7: Create ChatInteraction (pending)
        var interaction = ChatInteraction.Create(
            session.Id,
            activeState.OrganizationId,
            activeState.UserId,
            trimmedQuestion,
            questionHash,
            correlationId);

        await AuditAsync(
            AuditEventTypes.ChatInteractionStarted,
            $"Chat interaction started. SessionId={session.Id}",
            AuditSeverity.Info,
            activeState.OrganizationId,
            activeState.UserId,
            interaction.Id,
            cancellationToken);

        // Step 8: Execute authorized semantic retrieval
        var retrievalStopwatch = Stopwatch.StartNew();
        var retrievalResult = await retrievalService.RetrieveAsync(
            new EligibleSemanticRetrievalRequest(trimmedQuestion, TopK: 5),
            cancellationToken);
        retrievalStopwatch.Stop();
        var retrievalMs = retrievalStopwatch.ElapsedMilliseconds;

        var totalStopwatch = Stopwatch.StartNew();

        // Step 9: If retrieval is insufficient → record InsufficientContext, skip generator
        if (retrievalResult.IsInsufficientResult)
        {
            totalStopwatch.Stop();
            interaction.RecordInsufficientContextOutcome(
                retrievalResult.RetrievalQueryId,
                retrievalResult.ReturnedCount,
                retrievalMs,
                totalStopwatch.ElapsedMilliseconds);

            await AuditAsync(
                AuditEventTypes.InsufficientContextReturned,
                $"Insufficient retrieval context. SessionId={session.Id}",
                AuditSeverity.Info,
                activeState.OrganizationId,
                activeState.UserId,
                interaction.Id,
                cancellationToken);

            await PersistAsync(session, interaction, isNewSession, cancellationToken);

            return new AskQuestionResponse(
                interaction.Id,
                session.Id,
                AnswerState.InsufficientContext,
                AnswerText: InsufficientContextFallbackText,
                retrievalResult.ReturnedCount,
                IsInsufficientContext: true,
                correlationId);
        }

        // Step 10: If retrieval has a failure code → record ProviderFailed, skip generator
        if (retrievalResult.FailureCode is not null)
        {
            totalStopwatch.Stop();
            interaction.RecordProviderFailedOutcome(
                retrievalResult.FailureCode,
                retrievalResult.RetrievalQueryId,
                retrievalMs,
                generationMs: null,
                totalStopwatch.ElapsedMilliseconds);

            await AuditAsync(
                AuditEventTypes.ChatAnswerGenerationFailed,
                $"Retrieval failed before generation. SessionId={session.Id}",
                AuditSeverity.Warning,
                activeState.OrganizationId,
                activeState.UserId,
                interaction.Id,
                cancellationToken);

            await PersistAsync(session, interaction, isNewSession, cancellationToken);

            return new AskQuestionResponse(
                interaction.Id,
                session.Id,
                AnswerState.ProviderFailed,
                AnswerText: null,
                retrievalResult.ReturnedCount,
                IsInsufficientContext: false,
                correlationId);
        }

        // Step 11: Resolve chunk texts — fetch text for each authorized candidate
        var authorizedCandidates = retrievalResult.Candidates
            .Where(c => c.OrganizationId == activeState.OrganizationId)
            .ToArray();

        var chunkIds = authorizedCandidates.Select(c => c.ChunkId).ToArray();
        var chunkTexts = await chunkTextReader.GetChunkTextsAsync(chunkIds, activeState.OrganizationId, cancellationToken);

        var authorizedChunks = authorizedCandidates
            .Where(c => chunkTexts.ContainsKey(c.ChunkId))
            .Select(c => new AuthorizedChunkContext(
                c.ChunkId,
                c.DocumentId,
                c.OrganizationId,
                chunkTexts[c.ChunkId],
                c.ChunkIndex ?? 0,
                c.PageNumber,
                c.SectionLabel,
                c.RetrievalScore.Value))
            .ToArray();

        // Step 11a: Evaluate context sufficiency
        var sufficiencyResult = sufficiencyPolicy.Evaluate(authorizedChunks);
        if (!sufficiencyResult.IsSufficient)
        {
            totalStopwatch.Stop();
            interaction.RecordInsufficientContextOutcome(
                retrievalResult.RetrievalQueryId,
                authorizedCandidates.Length,
                retrievalMs,
                totalStopwatch.ElapsedMilliseconds);

            await AuditAsync(
                AuditEventTypes.InsufficientContextReturned,
                $"Context sufficiency policy returned insufficient. SessionId={session.Id} FailureCode={sufficiencyResult.FailureCode}",
                AuditSeverity.Info,
                activeState.OrganizationId,
                activeState.UserId,
                interaction.Id,
                cancellationToken);

            await PersistAsync(session, interaction, isNewSession, cancellationToken);

            return new AskQuestionResponse(
                interaction.Id,
                session.Id,
                AnswerState.InsufficientContext,
                AnswerText: InsufficientContextFallbackText,
                authorizedCandidates.Length,
                IsInsufficientContext: true,
                correlationId);
        }

        // Step 11b: Build grounded prompt
        var promptBuildResult = promptBuilder.Build(
            new GroundedPromptBuildRequest(trimmedQuestion, activeState.OrganizationId, authorizedChunks));

        if (!promptBuildResult.IsSuccess)
        {
            totalStopwatch.Stop();
            interaction.RecordProviderFailedOutcome(
                promptBuildResult.FailureCode ?? "PromptBuildFailed",
                retrievalResult.RetrievalQueryId,
                retrievalMs,
                generationMs: null,
                totalStopwatch.ElapsedMilliseconds);

            await AuditAsync(
                AuditEventTypes.PromptBuildFailed,
                $"Prompt build failed. SessionId={session.Id} IncludedChunkCount={promptBuildResult.IncludedChunkCount} ExcludedChunkCount={promptBuildResult.ExcludedChunkCount}",
                AuditSeverity.Warning,
                activeState.OrganizationId,
                activeState.UserId,
                interaction.Id,
                cancellationToken);

            await PersistAsync(session, interaction, isNewSession, cancellationToken);

            return new AskQuestionResponse(
                interaction.Id,
                session.Id,
                AnswerState.ProviderFailed,
                AnswerText: null,
                authorizedCandidates.Length,
                IsInsufficientContext: false,
                correlationId);
        }

        var builtPrompt = promptBuildResult.GroundedPrompt!;

        // Step 12: Call IAiAnswerGenerator with authorized chunks only
        var generationStopwatch = Stopwatch.StartNew();
        AnswerGenerationResult generationResult;
        try
        {
            generationResult = await answerGenerator.GenerateAsync(
                new AnswerGenerationRequest(
                    builtPrompt.AuthorizedChunksForGeneration,
                    trimmedQuestion,
                    PromptVersion: builtPrompt.PromptVersion,
                    ModelName: null),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            generationStopwatch.Stop();
            totalStopwatch.Stop();

            logger.LogWarning(
                "AI answer generation threw an exception. CorrelationId={CorrelationId} SessionId={SessionId}",
                correlationId,
                session.Id);

            interaction.RecordProviderFailedOutcome(
                "GenerationException",
                retrievalResult.RetrievalQueryId,
                retrievalMs,
                generationStopwatch.ElapsedMilliseconds,
                totalStopwatch.ElapsedMilliseconds);

            await AuditAsync(
                AuditEventTypes.ChatAnswerGenerationFailed,
                $"Answer generation failed with exception. SessionId={session.Id}",
                AuditSeverity.Error,
                activeState.OrganizationId,
                activeState.UserId,
                interaction.Id,
                cancellationToken);

            await PersistAsync(session, interaction, isNewSession, cancellationToken);

            return new AskQuestionResponse(
                interaction.Id,
                session.Id,
                AnswerState.ProviderFailed,
                AnswerText: null,
                authorizedCandidates.Length,
                IsInsufficientContext: false,
                correlationId);
        }

        generationStopwatch.Stop();
        totalStopwatch.Stop();

        // Step 13: Record grounded or provider-failed outcome
        if (generationResult.State == Domain.Chat.AnswerState.Grounded && generationResult.AnswerText is not null)
        {
            // Step 13.5: Map citations and track them in the shared EF context BEFORE recording
            // grounded outcome. Actual INSERT happens atomically with the chat_interaction row
            // in PersistAsync via a single SaveChangesAsync (EF topological ordering ensures
            // chat_interactions is inserted before citations, satisfying the FK constraint).
            var citations = await MapAndTrackCitationsAsync(
                interaction.Id,
                activeState.OrganizationId,
                builtPrompt.SourceHandles,
                cancellationToken);

            if (citations is null || citations.Count == 0)
            {
                // Citation mapping or persistence failed — grounded answer without citations is invalid.
                interaction.RecordProviderFailedOutcome(
                    "CitationMappingFailed",
                    retrievalResult.RetrievalQueryId,
                    retrievalMs,
                    generationStopwatch.ElapsedMilliseconds,
                    totalStopwatch.ElapsedMilliseconds);

                await AuditAsync(
                    AuditEventTypes.CitationMappingFailed,
                    $"Citation mapping produced no citations for grounded answer. SessionId={session.Id}",
                    AuditSeverity.Warning,
                    activeState.OrganizationId,
                    activeState.UserId,
                    interaction.Id,
                    cancellationToken);

                await PersistAsync(session, interaction, isNewSession, cancellationToken);

                return new AskQuestionResponse(
                    interaction.Id,
                    session.Id,
                    AnswerState.ProviderFailed,
                    AnswerText: null,
                    authorizedCandidates.Length,
                    IsInsufficientContext: false,
                    correlationId);
            }

            // Citations succeeded — now record the grounded outcome
            interaction.RecordGroundedOutcome(
                generationResult.AnswerText,
                retrievalResult.RetrievalQueryId,
                authorizedCandidates.Length,
                retrievalMs,
                generationStopwatch.ElapsedMilliseconds,
                totalStopwatch.ElapsedMilliseconds,
                generationResult.InputTokens,
                generationResult.OutputTokens,
                cost: null,
                generationResult.ProviderName,
                generationResult.ModelUsed,
                promptVersion: builtPrompt.PromptVersion);

            await AuditAsync(
                AuditEventTypes.ChatAnswerGenerationCompleted,
                $"Answer generation completed. SessionId={session.Id} CandidateCount={authorizedCandidates.Length}",
                AuditSeverity.Info,
                activeState.OrganizationId,
                activeState.UserId,
                interaction.Id,
                cancellationToken);

            // Step 14: Persist session + interaction + tracked citations atomically.
            // interactionRepository.SaveChangesAsync() flushes all EF-tracked changes (interaction
            // and citations) in one round-trip, satisfying the chat_interaction FK before citations.
            await PersistAsync(session, interaction, isNewSession, cancellationToken);

            await AuditAsync(
                AuditEventTypes.CitationsPersisted,
                $"Citations persisted. InteractionId={interaction.Id} CitationCount={citations.Count}",
                AuditSeverity.Info,
                activeState.OrganizationId,
                activeState.UserId,
                interaction.Id,
                cancellationToken);

            // Step 15: Return grounded response with citations (all already org-scoped by mapper)
            var citationResponses = citations
                .Where(c => citationAuthorizationFilter.IsCitationAuthorizedForUser(c.OrganizationId, activeState.OrganizationId))
                .Select(c => new CitationResponse(
                    c.DocumentId,
                    c.ChunkId,
                    c.Rank,
                    c.DocumentTitle,
                    c.PageNumber,
                    c.SectionLabel,
                    c.RelevanceScore))
                .ToArray();

            return new AskQuestionResponse(
                interaction.Id,
                session.Id,
                interaction.AnswerState,
                interaction.AnswerText,
                interaction.RetrievalCandidateCount,
                IsInsufficientContext: false,
                correlationId,
                Citations: citationResponses);
        }

        // Provider-failed path (from generation)
        interaction.RecordProviderFailedOutcome(
            generationResult.SafeFailureCode,
            retrievalResult.RetrievalQueryId,
            retrievalMs,
            generationStopwatch.ElapsedMilliseconds,
            totalStopwatch.ElapsedMilliseconds);

        await AuditAsync(
            AuditEventTypes.ChatAnswerGenerationFailed,
            $"Answer generation returned provider failure. SessionId={session.Id}",
            AuditSeverity.Warning,
            activeState.OrganizationId,
            activeState.UserId,
            interaction.Id,
            cancellationToken);

        await PersistAsync(session, interaction, isNewSession, cancellationToken);

        return new AskQuestionResponse(
            interaction.Id,
            session.Id,
            interaction.AnswerState,
            interaction.AnswerText,
            interaction.RetrievalCandidateCount,
            IsInsufficientContext: false,
            correlationId);
    }

    private async Task PersistAsync(
        ChatSession session,
        ChatInteraction interaction,
        bool isNewSession,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        session.UpdateLastInteractionAt(now);

        if (isNewSession)
            await sessionRepository.AddAsync(session, cancellationToken);
        else
            await sessionRepository.SaveChangesAsync(cancellationToken);

        await interactionRepository.AddAsync(interaction, cancellationToken);
        await interactionRepository.SaveChangesAsync(cancellationToken);

        await AuditAsync(
            AuditEventTypes.ChatInteractionStored,
            $"Chat interaction stored. SessionId={session.Id} InteractionId={interaction.Id}",
            AuditSeverity.Info,
            interaction.OrganizationId,
            interaction.UserId,
            interaction.Id,
            cancellationToken);
    }

    private async Task<IReadOnlyList<Citation>?> MapAndTrackCitationsAsync(
        Guid interactionId,
        Guid organizationId,
        IReadOnlyList<PromptSourceHandle> sourceHandles,
        CancellationToken cancellationToken)
    {
        if (sourceHandles.Count == 0)
            return null;

        var mappingSources = sourceHandles
            .Select(h => new CitationMappingSource(
                h.DocumentId,
                h.ChunkId,
                h.Rank,
                h.RelevanceScore,
                h.PageNumber,
                h.SectionLabel))
            .ToArray();

        IReadOnlyList<Citation> citations;
        try
        {
            citations = await citationMapper.MapAsync(interactionId, organizationId, mappingSources, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                "Citation mapping failed. InteractionId={InteractionId} CorrelationId={CorrelationId}",
                interactionId,
                correlationContext.CorrelationId);
            return null;
        }

        if (citations.Count == 0)
            return null;

        try
        {
            // Track citations in the shared EF context. SaveChangesAsync is NOT called here;
            // the actual INSERT is deferred to PersistAsync which saves the interaction and
            // citations together in one db.SaveChangesAsync(), satisfying the FK constraint.
            await citationRepository.AddRangeAsync(citations, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                "Citation tracking failed. InteractionId={InteractionId} CorrelationId={CorrelationId}",
                interactionId,
                correlationContext.CorrelationId);
            return null;
        }

        return citations;
    }

    private static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task AuditAsync(
        string eventType,
        string message,
        AuditSeverity severity,
        Guid organizationId,
        Guid userId,
        Guid interactionId,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditWriter.WriteAsync(
                new AuditEvent(
                    eventType,
                    message,
                    severity,
                    correlationContext.CorrelationId,
                    organizationId,
                    userId,
                    "ChatInteraction",
                    interactionId),
                cancellationToken);
        }
        catch
        {
            logger.LogWarning(
                "Chat audit write failed. EventType={EventType} CorrelationId={CorrelationId}",
                eventType,
                correlationContext.CorrelationId);
        }
    }
}

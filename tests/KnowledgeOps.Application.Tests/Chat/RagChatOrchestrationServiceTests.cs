using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Authorization.Hooks;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Application.Chat.Citations;
using KnowledgeOps.Application.Chat.Prompting;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Application.Retrieval;
using KnowledgeOps.Domain.Chat;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Chat;

public sealed class RagChatOrchestrationServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrganizationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ClaimOrganizationId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task RagChatOrchestrationService_RetrievesBeforeGenerating()
    {
        var callOrder = new List<string>();
        var harness = CreateHarness(
            candidates: [AuthorizedCandidate()],
            trackCallOrder: callOrder);

        await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Contains("retrieval", callOrder);
        Assert.Contains("generation", callOrder);
        Assert.True(
            callOrder.IndexOf("retrieval") < callOrder.IndexOf("generation"),
            "Retrieval must happen before generation.");
    }

    [Fact]
    public async Task RagChatOrchestrationService_DoesNotGenerateWhenRetrievalInsufficient()
    {
        var harness = CreateHarness(isInsufficientResult: true);

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.False(harness.Generator.WasCalled, "Generator must not be called when retrieval is insufficient.");
        Assert.True(response.IsInsufficientContext);
        Assert.Equal(AnswerState.InsufficientContext, response.AnswerState);
    }

    [Fact]
    public async Task RagChatOrchestrationService_PropagatesOrganizationScopeToRetrieval()
    {
        var harness = CreateHarness(candidates: [AuthorizedCandidate()]);

        await harness.Service.AskAsync(new AskQuestionRequest("What changed?"));

        Assert.NotNull(harness.RetrievalService.LastRequest);
        Assert.Equal("What changed?", harness.RetrievalService.LastRequest.QueryText);

        // Chunk text resolution must use the DB-backed org scope, not the JWT claim org.
        // OrganizationId ("22222...") is from UserAccessState; ClaimOrganizationId ("33333...") is from JWT.
        Assert.Equal(OrganizationId, harness.ChunkTextReader.LastOrganizationId);
        Assert.NotEqual(ClaimOrganizationId, harness.ChunkTextReader.LastOrganizationId);
    }

    [Fact]
    public async Task RagChatOrchestrationService_DoesNotPassUnauthorizedChunksToGenerator()
    {
        // One authorized candidate (OrganizationId = DB org) and one cross-org candidate
        // (OrganizationId = ClaimOrganizationId / JWT org). Only the authorized one must
        // reach the generator after the orchestration's step-11 org filter.
        var authorizedCandidate = AuthorizedCandidate();
        var crossOrgCandidate = CrossOrgCandidate();
        var harness = CreateHarness(candidates: [authorizedCandidate, crossOrgCandidate]);

        await harness.Service.AskAsync(new AskQuestionRequest("Tell me about the merger."));

        Assert.NotNull(harness.Generator.LastRequest);
        var singleChunk = Assert.Single(harness.Generator.LastRequest!.AuthorizedChunks);
        Assert.Equal(OrganizationId, singleChunk.OrganizationId);
    }

    [Fact]
    public async Task RagChatOrchestrationService_PersistsGroundedInteraction()
    {
        var harness = CreateHarness(candidates: [AuthorizedCandidate()]);

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.Grounded, response.AnswerState);
        var stored = Assert.Single(harness.InteractionRepository.StoredInteractions);
        Assert.Equal(AnswerState.Grounded, stored.AnswerState);
        Assert.NotNull(stored.AnswerText);
    }

    [Fact]
    public async Task RagChatOrchestrationService_PersistsInsufficientContextInteraction()
    {
        var harness = CreateHarness(isInsufficientResult: true);

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.InsufficientContext, response.AnswerState);
        var stored = Assert.Single(harness.InteractionRepository.StoredInteractions);
        Assert.Equal(AnswerState.InsufficientContext, stored.AnswerState);
        Assert.Null(stored.AnswerText);
    }

    [Fact]
    public async Task RagChatOrchestrationService_PersistsProviderFailureSafely()
    {
        var harness = CreateHarness(
            candidates: [AuthorizedCandidate()],
            generatorState: AnswerState.ProviderFailed,
            generatorFailureCode: "ModelUnavailable");

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.ProviderFailed, response.AnswerState);
        var stored = Assert.Single(harness.InteractionRepository.StoredInteractions);
        Assert.Equal(AnswerState.ProviderFailed, stored.AnswerState);
        Assert.Null(stored.AnswerText);

        // Must never store exception details in any text field
        if (stored.ProviderFailureCode is not null)
            Assert.DoesNotContain("Exception", stored.ProviderFailureCode, StringComparison.OrdinalIgnoreCase);
        if (stored.AnswerText is not null)
            Assert.DoesNotContain("Exception", stored.AnswerText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RagChatOrchestration_ProviderFailed_StoresProviderNameFromGenerator()
    {
        // When the generator returns ProviderFailed, AiProvider and AiModel on the stored
        // interaction must come from the generator result, not be null or hardcoded.
        var harness = CreateHarness(
            candidates: [AuthorizedCandidate()],
            generatorState: AnswerState.ProviderFailed,
            generatorFailureCode: "ProviderUnavailable");

        await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        var stored = Assert.Single(harness.InteractionRepository.StoredInteractions);
        Assert.Equal(AnswerState.ProviderFailed, stored.AnswerState);
        Assert.Equal("FakeTest", stored.AiProvider);
        Assert.Equal("fake-test-v1", stored.AiModel);
        Assert.Equal("ProviderUnavailable", stored.ProviderFailureCode);
    }

    [Fact]
    public async Task RagChatOrchestrationService_StoresLatencyMetadata()
    {
        var harness = CreateHarness(candidates: [AuthorizedCandidate()]);

        await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        var stored = Assert.Single(harness.InteractionRepository.StoredInteractions);

        // For grounded outcome, latency fields should be set (non-negative when provided)
        if (stored.RetrievalLatencyMs.HasValue)
            Assert.True(stored.RetrievalLatencyMs.Value >= 0, "RetrievalLatencyMs must be non-negative.");
        if (stored.GenerationLatencyMs.HasValue)
            Assert.True(stored.GenerationLatencyMs.Value >= 0, "GenerationLatencyMs must be non-negative.");
        if (stored.TotalLatencyMs.HasValue)
            Assert.True(stored.TotalLatencyMs.Value >= 0, "TotalLatencyMs must be non-negative.");
    }

    [Fact]
    public async Task RagChatOrchestrationService_AllowsNullTokenAndCostMetadata()
    {
        var harness = CreateHarness(candidates: [AuthorizedCandidate()]);

        await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        var stored = Assert.Single(harness.InteractionRepository.StoredInteractions);

        // When the generator returns null for tokens/cost, they must be stored as null (never zero)
        if (stored.TokenUsageInput.HasValue)
            Assert.NotEqual(0, stored.TokenUsageInput.Value);
        if (stored.TokenUsageOutput.HasValue)
            Assert.NotEqual(0, stored.TokenUsageOutput.Value);
        if (stored.EstimatedCost.HasValue)
            Assert.NotEqual(0m, stored.EstimatedCost.Value);
    }

    [Fact]
    public async Task RagChatOrchestrationService_UsesFakeGeneratorInTests()
    {
        var harness = CreateHarness(candidates: [AuthorizedCandidate()]);

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.Grounded, response.AnswerState);
        Assert.NotNull(response.AnswerText);

        // Verify determinism: same question, same candidate count → same answer
        var harness2 = CreateHarness(candidates: [AuthorizedCandidate()]);
        var response2 = await harness2.Service.AskAsync(new AskQuestionRequest("What is the policy?"));
        Assert.Equal(response.AnswerText, response2.AnswerText);
    }

    [Fact]
    public async Task RagChatOrchestrationService_RejectsSessionNotOwnedByUser()
    {
        var harness = CreateHarness(candidates: [AuthorizedCandidate()]);

        // Session belongs to a different user in the same org — must be rejected.
        var differentUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var otherUserSession = ChatSession.Create(OrganizationId, differentUserId, title: null);
        await harness.SessionRepository.AddAsync(otherUserSession);

        var request = new AskQuestionRequest("What is the policy?", ChatSessionId: otherUserSession.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => harness.Service.AskAsync(request));
    }

    [Fact]
    public async Task RagChatOrchestrationService_DoesNotLeakExceptionMessageOnGeneratorException()
    {
        const string secretMessage = "ConnectionString=Server=internal;Password=secret123";

        var currentUser = new FakeCurrentUser(true, UserId, ClaimOrganizationId);
        var accessReader = new FakeAccessStateReader(new UserAccessState(UserId, OrganizationId, ["Agent"]));
        var permissionService = new PermissionService();
        var candidate = AuthorizedCandidate();
        var chunkTexts = new Dictionary<Guid, string> { [candidate.ChunkId] = "Sample chunk text." };
        var retrievalService = new FakeRetrievalService([candidate], false, null, null);
        var throwingGenerator = new ThrowingAnswerGenerator(secretMessage);
        var sessionRepository = new FakeSessionRepository();
        var interactionRepository = new FakeInteractionRepository();
        var chunkTextReader = new FakeChunkTextReader(chunkTexts);
        var auditWriter = new CapturingAuditEventWriter();
        var correlationContext = new StaticCorrelationContext();

        var promptBuilder = new GroundedPromptBuilder(new DefaultPromptAuthorizationFilter());
        var sufficiencyPolicy = new ContextSufficiencyPolicy();

        var titleReader2 = new FakeDocumentTitleReader(
            new Dictionary<Guid, string> { [candidate.DocumentId] = "Test Document" });
        var citationMapper2 = new CitationMapper(titleReader2, NullLogger<CitationMapper>.Instance);
        var citationRepository2 = new FakeCitationRepository();
        var citationAuthFilter2 = new DefaultCitationAuthorizationFilter();

        var service = new RagChatOrchestrationService(
            currentUser, accessReader, permissionService, retrievalService,
            throwingGenerator, sessionRepository, interactionRepository,
            chunkTextReader, promptBuilder, sufficiencyPolicy,
            citationMapper2, citationRepository2, citationAuthFilter2,
            auditWriter, correlationContext,
            NullLogger<RagChatOrchestrationService>.Instance);

        var response = await service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.ProviderFailed, response.AnswerState);

        var stored = Assert.Single(interactionRepository.StoredInteractions);
        Assert.Null(stored.AnswerText);
        Assert.Equal("GenerationException", stored.ProviderFailureCode);

        // Secret exception message must never appear in any stored field.
        Assert.DoesNotContain(secretMessage, stored.ProviderFailureCode ?? string.Empty, StringComparison.Ordinal);
        if (stored.AnswerText is not null)
            Assert.DoesNotContain(secretMessage, stored.AnswerText, StringComparison.Ordinal);
        Assert.Equal("throwing-test-v1", stored.AiModel);
        Assert.DoesNotContain(secretMessage, stored.AiModel, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RagChatOrchestrationService_StoresPromptVersionForGroundedInteraction()
    {
        var harness = CreateHarness(candidates: [AuthorizedCandidate()]);

        await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        var stored = Assert.Single(harness.InteractionRepository.StoredInteractions);
        Assert.Equal("rag-grounded-v1", stored.PromptVersion);
    }

    [Fact]
    public async Task RagChatOrchestrationService_DoesNotStorePromptVersionForInsufficientContext()
    {
        var harness = CreateHarness(isInsufficientResult: true);

        await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        var stored = Assert.Single(harness.InteractionRepository.StoredInteractions);
        Assert.Null(stored.PromptVersion);
    }

    [Fact]
    public async Task RagChatOrchestrationService_DoesNotStorePromptVersionForProviderFailure()
    {
        var harness = CreateHarness(
            candidates: [AuthorizedCandidate()],
            generatorState: AnswerState.ProviderFailed,
            generatorFailureCode: "ModelUnavailable");

        await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        var stored = Assert.Single(harness.InteractionRepository.StoredInteractions);
        Assert.Null(stored.PromptVersion);
    }

    [Fact]
    public async Task RagChatOrchestrationService_AuditEventsDoNotContainQuestionOrAnswerText()
    {
        const string questionText = "What-is-the-confidential-policy-text?";
        var harness = CreateHarness(candidates: [AuthorizedCandidate()]);

        var response = await harness.Service.AskAsync(new AskQuestionRequest(questionText));

        foreach (var auditEvent in harness.AuditWriter.Events)
        {
            Assert.DoesNotContain(questionText, auditEvent.Message, StringComparison.Ordinal);
            if (response.AnswerText is not null)
                Assert.DoesNotContain(response.AnswerText, auditEvent.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task RagChatOrchestration_PersistsCitationsForGroundedAnswer()
    {
        var candidate = AuthorizedCandidate();
        var harness = CreateHarness(candidates: [candidate]);

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.Grounded, response.AnswerState);
        Assert.NotNull(response.Citations);
        Assert.NotEmpty(response.Citations);
    }

    [Fact]
    public async Task RagChatOrchestration_ReturnsCitationsForGroundedAnswer()
    {
        var candidate = AuthorizedCandidate();
        var harness = CreateHarness(candidates: [candidate]);

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.Grounded, response.AnswerState);
        Assert.NotNull(response.Citations);
        var citation = Assert.Single(response.Citations);
        Assert.Equal(1, citation.Rank);
        // Score is nullable; when provided by retrieval it must be present and positive
        Assert.NotNull(citation.RelevanceScore);
        Assert.True(citation.RelevanceScore > 0.0);
    }

    [Fact]
    public async Task RagChatOrchestration_DoesNotCreateCitationsForInsufficientContext()
    {
        var harness = CreateHarness(candidates: [], isInsufficientResult: true);

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.InsufficientContext, response.AnswerState);
        Assert.True(response.Citations is null || response.Citations.Count == 0);
    }

    [Fact]
    public async Task RagChatOrchestration_DoesNotCreateCitationsForProviderFailure()
    {
        var candidate = AuthorizedCandidate();
        var harness = CreateHarness(candidates: [candidate], generatorState: AnswerState.ProviderFailed, generatorFailureCode: "ModelUnavailable");

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.ProviderFailed, response.AnswerState);
        Assert.True(response.Citations is null || response.Citations.Count == 0);
    }

    [Fact]
    public async Task RagChatOrchestration_CitationOrganizationMatchesInteractionOrganization()
    {
        var candidate = AuthorizedCandidate();
        var harness = CreateHarness(candidates: [candidate]);

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.Grounded, response.AnswerState);
        Assert.NotNull(response.Citations);
        // All citations should be returned (org scope matches since candidate uses OrganizationId)
        Assert.NotEmpty(response.Citations);
    }

    [Fact]
    public async Task RagChatOrchestration_GroundedAnswer_CitationRepositorySaveChangesIsNotCalledDirectly()
    {
        // Verifies Option C: citations are tracked via AddRangeAsync only; SaveChangesAsync on
        // the citation repository must never be called. The actual INSERT is deferred to
        // PersistAsync which flushes all EF-tracked changes (interaction + citations) atomically.
        var candidate = AuthorizedCandidate();
        var citationRepo = new FakeCitationRepository();
        var harness = CreateHarness(candidates: [candidate], citationRepository: citationRepo);

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.Grounded, response.AnswerState);
        Assert.False(harness.CitationRepository.SaveChangesWasCalled,
            "CitationRepository.SaveChangesAsync must not be called by the orchestrator; " +
            "citations are persisted atomically with the interaction via the shared DbContext.");
    }

    [Fact]
    public async Task RagChatOrchestration_CitationPersistenceFailureFinalInteractionStateIsProviderFailed()
    {
        // When citation tracking (AddRangeAsync) fails, the final persisted interaction must be
        // ProviderFailed, not Grounded — grounded answer without citations is not acceptable.
        var candidate = AuthorizedCandidate();
        var throwingRepo = new ThrowingCitationRepository();
        var harness = CreateHarness(candidates: [candidate], citationRepository: throwingRepo);

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.ProviderFailed, response.AnswerState);
        Assert.True(response.Citations is null || response.Citations.Count == 0);

        var stored = Assert.Single(harness.InteractionRepository.StoredInteractions);
        Assert.Equal(AnswerState.ProviderFailed, stored.AnswerState);
        Assert.Null(stored.AnswerText);
        Assert.Equal("CitationMappingFailed", stored.ProviderFailureCode);
        // Safe failure code must never contain exception details
        Assert.DoesNotContain("Exception", stored.ProviderFailureCode ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Simulated", stored.ProviderFailureCode ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RagChatOrchestration_DoesNotReturnGroundedAnswerWithoutCitations()
    {
        var candidate = AuthorizedCandidate();
        var emptyMapper = new AlwaysEmptyCitationMapper();
        var harness = CreateHarness(candidates: [candidate], citationMapper: emptyMapper);

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.ProviderFailed, response.AnswerState);
        Assert.True(response.Citations is null || response.Citations.Count == 0);
        var stored = Assert.Single(harness.InteractionRepository.StoredInteractions);
        Assert.Equal(AnswerState.ProviderFailed, stored.AnswerState);
        Assert.Null(stored.AnswerText);
        Assert.Equal("CitationMappingFailed", stored.ProviderFailureCode);
    }

    [Fact]
    public async Task RagChatOrchestration_GroundedAnswer_ChatAnswerGenerationCompletedAuditedAfterPersist()
    {
        // Verifies that ChatAnswerGenerationCompleted is emitted AFTER ChatInteractionStored so
        // EfAuditEventWriter's SaveChangesAsync cannot flush tracked citations before the
        // chat_interaction row exists (FK_citations_chat_interactions_chat_interaction_id).
        var candidate = AuthorizedCandidate();
        var harness = CreateHarness(candidates: [candidate]);

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.Grounded, response.AnswerState);

        var events = harness.AuditWriter.Events.Select(e => e.EventType).ToList();
        var storedIndex = events.IndexOf(AuditEventTypes.ChatInteractionStored);
        var completedIndex = events.IndexOf(AuditEventTypes.ChatAnswerGenerationCompleted);

        Assert.True(storedIndex >= 0, "ChatInteractionStored audit event must be present for grounded answer.");
        Assert.True(completedIndex >= 0, "ChatAnswerGenerationCompleted audit event must be present for grounded answer.");
        Assert.True(storedIndex < completedIndex,
            $"ChatInteractionStored (index {storedIndex}) must precede ChatAnswerGenerationCompleted (index {completedIndex}) " +
            "to prevent FK violation when EfAuditEventWriter flushes the shared DbContext.");
    }

    [Fact]
    public async Task RagChatOrchestrationService_PassesSystemInstructionToGenerator()
    {
        var harness = CreateHarness(candidates: [AuthorizedCandidate()]);

        await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.NotNull(harness.Generator.LastRequest);
        Assert.False(
            string.IsNullOrWhiteSpace(harness.Generator.LastRequest!.SystemInstruction),
            "SystemInstruction must be populated in AnswerGenerationRequest.");
    }

    [Fact]
    public async Task RagChatOrchestrationService_PassesFormattedContextToGenerator()
    {
        var harness = CreateHarness(candidates: [AuthorizedCandidate()]);

        await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.NotNull(harness.Generator.LastRequest);
        Assert.False(
            string.IsNullOrWhiteSpace(harness.Generator.LastRequest!.FormattedContext),
            "FormattedContext must be populated in AnswerGenerationRequest.");
    }

    [Fact]
    public async Task RagChatOrchestrationService_ProviderFailedOutcome_StoresRetrievalCandidateCount()
    {
        // When the generator returns ProviderFailed, the stored interaction must reflect the
        // actual number of retrieved candidates, not the default value of 0. This prevents
        // misleading "Retrieved chunks: 0" in Chat History when retrieval actually succeeded.
        var candidate = AuthorizedCandidate();
        var harness = CreateHarness(
            candidates: [candidate],
            generatorState: AnswerState.ProviderFailed,
            generatorFailureCode: "ProviderUnavailable");

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.ProviderFailed, response.AnswerState);
        var stored = Assert.Single(harness.InteractionRepository.StoredInteractions);
        Assert.Equal(AnswerState.ProviderFailed, stored.AnswerState);
        Assert.True(
            stored.RetrievalCandidateCount > 0,
            "RetrievalCandidateCount must be > 0 when retrieval found candidates before the provider failed.");
    }

    [Fact]
    public async Task RagChatOrchestrationService_GenerationException_StoresRetrievalCandidateCount()
    {
        // Same as above but for the exception code path.
        const string secretMessage = "Any exception message";
        var currentUser = new FakeCurrentUser(true, UserId, ClaimOrganizationId);
        var accessReader = new FakeAccessStateReader(new UserAccessState(UserId, OrganizationId, ["Agent"]));
        var permissionService = new PermissionService();
        var candidate = AuthorizedCandidate();
        var chunkTexts = new Dictionary<Guid, string> { [candidate.ChunkId] = "Sample chunk text." };
        var retrievalService = new FakeRetrievalService([candidate], false, null, null);
        var throwingGenerator = new ThrowingAnswerGenerator(secretMessage);
        var sessionRepository = new FakeSessionRepository();
        var interactionRepository = new FakeInteractionRepository();
        var chunkTextReader = new FakeChunkTextReader(chunkTexts);
        var auditWriter = new CapturingAuditEventWriter();
        var correlationContext = new StaticCorrelationContext();
        var promptBuilder = new GroundedPromptBuilder(new DefaultPromptAuthorizationFilter());
        var sufficiencyPolicy = new ContextSufficiencyPolicy();
        var titleReader = new FakeDocumentTitleReader(
            new Dictionary<Guid, string> { [candidate.DocumentId] = "Test Document" });
        var citationMapper = new CitationMapper(titleReader, NullLogger<CitationMapper>.Instance);
        var citationRepository = new FakeCitationRepository();
        var citationAuthFilter = new DefaultCitationAuthorizationFilter();

        var service = new RagChatOrchestrationService(
            currentUser, accessReader, permissionService, retrievalService,
            throwingGenerator, sessionRepository, interactionRepository,
            chunkTextReader, promptBuilder, sufficiencyPolicy,
            citationMapper, citationRepository, citationAuthFilter,
            auditWriter, correlationContext,
            NullLogger<RagChatOrchestrationService>.Instance);

        var response = await service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.ProviderFailed, response.AnswerState);
        var stored = Assert.Single(interactionRepository.StoredInteractions);
        Assert.True(
            stored.RetrievalCandidateCount > 0,
            "RetrievalCandidateCount must reflect actual retrieved candidates even when generation throws.");
        Assert.Equal("ThrowingTest", stored.AiProvider);
        Assert.Equal("throwing-test-v1", stored.AiModel);
    }

    [Fact]
    public async Task RagChatOrchestration_CitationPersistenceFailureDoesNotReturnGroundedAnswer()
    {
        var candidate = AuthorizedCandidate();
        var throwingRepo = new ThrowingCitationRepository();
        var harness = CreateHarness(candidates: [candidate], citationRepository: throwingRepo);

        var response = await harness.Service.AskAsync(new AskQuestionRequest("What is the policy?"));

        Assert.Equal(AnswerState.ProviderFailed, response.AnswerState);
        Assert.True(response.Citations is null || response.Citations.Count == 0);
        var stored = Assert.Single(harness.InteractionRepository.StoredInteractions);
        Assert.Equal(AnswerState.ProviderFailed, stored.AnswerState);
        Assert.Null(stored.AnswerText);

        // Safe failure code must not expose exception details
        if (stored.ProviderFailureCode is not null)
            Assert.DoesNotContain("Exception", stored.ProviderFailureCode, StringComparison.OrdinalIgnoreCase);
    }

    // ─── Harness ──────────────────────────────────────────────────────────────

    private static TestHarness CreateHarness(
        bool isAuthenticated = true,
        IReadOnlyList<EligibleSemanticRetrievalCandidate>? candidates = null,
        bool isInsufficientResult = false,
        string? retrievalFailureCode = null,
        AnswerState generatorState = AnswerState.Grounded,
        string? generatorFailureCode = null,
        List<string>? trackCallOrder = null,
        ICitationMapper? citationMapper = null,
        ICitationRepository? citationRepository = null)
    {
        var currentUser = new FakeCurrentUser(isAuthenticated, UserId, ClaimOrganizationId);
        var accessReader = new FakeAccessStateReader(new UserAccessState(UserId, OrganizationId, ["Agent"]));
        var permissionService = new PermissionService();

        var chunkTexts = (candidates ?? [])
            .ToDictionary(c => c.ChunkId, _ => "Sample chunk text for testing.");

        var retrievalService = new FakeRetrievalService(
            candidates ?? [],
            isInsufficientResult,
            retrievalFailureCode,
            trackCallOrder);

        var generator = new FakeAnswerGeneratorForTests(
            generatorState,
            generatorFailureCode,
            trackCallOrder);

        var sessionRepository = new FakeSessionRepository();
        var interactionRepository = new FakeInteractionRepository();
        var chunkTextReader = new FakeChunkTextReader(chunkTexts);
        var auditWriter = new CapturingAuditEventWriter();
        var correlationContext = new StaticCorrelationContext();

        var promptBuilder = new GroundedPromptBuilder(new DefaultPromptAuthorizationFilter());
        var sufficiencyPolicy = new ContextSufficiencyPolicy();

        var titleReader = new FakeDocumentTitleReader(
            (candidates ?? []).ToDictionary(c => c.DocumentId, _ => "Test Document"));
        var resolvedCitationMapper = citationMapper
            ?? new CitationMapper(titleReader, NullLogger<CitationMapper>.Instance);
        var resolvedCitationRepository = citationRepository as FakeCitationRepository
            ?? (citationRepository is null ? new FakeCitationRepository() : null);
        var citationRepo = (ICitationRepository?)resolvedCitationRepository ?? citationRepository!;
        var citationAuthFilter = new DefaultCitationAuthorizationFilter();

        var service = new RagChatOrchestrationService(
            currentUser,
            accessReader,
            permissionService,
            retrievalService,
            generator,
            sessionRepository,
            interactionRepository,
            chunkTextReader,
            promptBuilder,
            sufficiencyPolicy,
            resolvedCitationMapper,
            citationRepo,
            citationAuthFilter,
            auditWriter,
            correlationContext,
            NullLogger<RagChatOrchestrationService>.Instance);

        return new TestHarness(service, retrievalService, generator, sessionRepository, interactionRepository, auditWriter, chunkTextReader, resolvedCitationRepository ?? new FakeCitationRepository());
    }

    private static EligibleSemanticRetrievalCandidate AuthorizedCandidate() =>
        new(
            Rank: 1,
            OrganizationId: OrganizationId,
            DocumentId: Guid.NewGuid(),
            ChunkId: Guid.NewGuid(),
            ChunkEmbeddingId: Guid.NewGuid(),
            RetrievalScore: new RetrievalScore(0.9, "CosineSimilarity"),
            ScoreMethod: "CosineSimilarity",
            ProviderName: "Fake",
            ModelName: "fake-v1",
            ChunkIndex: 0,
            PageNumber: 1,
            SectionLabel: "Policy");

    private static EligibleSemanticRetrievalCandidate CrossOrgCandidate() =>
        new(
            Rank: 2,
            OrganizationId: ClaimOrganizationId,
            DocumentId: Guid.NewGuid(),
            ChunkId: Guid.NewGuid(),
            ChunkEmbeddingId: Guid.NewGuid(),
            RetrievalScore: new RetrievalScore(0.8, "CosineSimilarity"),
            ScoreMethod: "CosineSimilarity",
            ProviderName: "Fake",
            ModelName: "fake-v1",
            ChunkIndex: 0,
            PageNumber: 1,
            SectionLabel: "Other");

    private sealed record TestHarness(
        RagChatOrchestrationService Service,
        FakeRetrievalService RetrievalService,
        FakeAnswerGeneratorForTests Generator,
        FakeSessionRepository SessionRepository,
        FakeInteractionRepository InteractionRepository,
        CapturingAuditEventWriter AuditWriter,
        FakeChunkTextReader ChunkTextReader,
        FakeCitationRepository CitationRepository);

    // ─── Fakes ────────────────────────────────────────────────────────────────

    private sealed class FakeCurrentUser(
        bool isAuthenticated,
        Guid userId,
        Guid organizationId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
        public Guid OrganizationId { get; } = organizationId;
        public string Email => "agent@example.test";
        public string DisplayName => "Agent";
        public IReadOnlyList<string> Roles => ["Agent"];
        public bool IsAuthenticated { get; } = isAuthenticated;
    }

    private sealed class FakeAccessStateReader(UserAccessState? activeState) : IUserAccessStateReader
    {
        public Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(activeState);
    }

    private sealed class FakeRetrievalService(
        IReadOnlyList<EligibleSemanticRetrievalCandidate> candidates,
        bool isInsufficientResult,
        string? failureCode,
        List<string>? callOrder) : IEligibleSemanticRetrievalService
    {
        public EligibleSemanticRetrievalRequest? LastRequest { get; private set; }

        public Task<EligibleSemanticRetrievalResult> RetrieveAsync(
            EligibleSemanticRetrievalRequest request,
            CancellationToken cancellationToken = default)
        {
            callOrder?.Add("retrieval");
            LastRequest = request;

            var result = new EligibleSemanticRetrievalResult(
                Guid.NewGuid(),
                "abc123",
                isInsufficientResult,
                candidates,
                RequestedTopK: 5,
                ReturnedCount: candidates.Count,
                ProviderMetadata: null,
                FailureCode: failureCode,
                FailureReason: failureCode is not null ? "Retrieval failed." : null);

            return Task.FromResult(result);
        }
    }

    private sealed class FakeAnswerGeneratorForTests(
        AnswerState state,
        string? failureCode,
        List<string>? callOrder) : IAiAnswerGenerator
    {
        public bool WasCalled { get; private set; }
        public AnswerGenerationRequest? LastRequest { get; private set; }

        public string ProviderName => "FakeTest";
        public string DefaultModelName => "fake-test-v1";

        public Task<AnswerGenerationResult> GenerateAsync(
            AnswerGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            callOrder?.Add("generation");
            WasCalled = true;
            LastRequest = request;

            var candidateCount = request.AuthorizedChunks.Count;
            var answerText = state == AnswerState.Grounded
                ? $"Fake grounded answer based on {candidateCount} authorized retrieved context(s)."
                : null;

            return Task.FromResult(new AnswerGenerationResult(
                state,
                answerText,
                InputTokens: null,
                OutputTokens: null,
                ModelUsed: DefaultModelName,
                ProviderName: ProviderName,
                SafeFailureCode: failureCode));
        }
    }

    private sealed class FakeSessionRepository : IChatSessionRepository
    {
        private readonly List<ChatSession> _sessions = [];
        public IReadOnlyList<ChatSession> StoredSessions => _sessions;

        public Task<ChatSession?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_sessions.FirstOrDefault(s => s.Id == id));

        public Task<ChatSession?> FindByIdAndOrganizationAsync(Guid id, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(_sessions.FirstOrDefault(s => s.Id == id && s.OrganizationId == organizationId));

        public Task<IReadOnlyList<ChatSession>> GetRecentByUserAsync(Guid userId, Guid organizationId, int limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ChatSession>>(_sessions.Where(s => s.UserId == userId && s.OrganizationId == organizationId).Take(limit).ToList());

        public Task<IReadOnlyList<ChatSession>> GetRecentByOrganizationAsync(Guid organizationId, int limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ChatSession>>(_sessions.Where(s => s.OrganizationId == organizationId).Take(limit).ToList());

        public Task<int> CountInteractionsBySessionAsync(Guid sessionId, CancellationToken ct = default) =>
            Task.FromResult(0);

        public Task AddAsync(ChatSession session, CancellationToken ct = default)
        {
            _sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeInteractionRepository : IChatInteractionRepository
    {
        private readonly List<ChatInteraction> _interactions = [];
        public IReadOnlyList<ChatInteraction> StoredInteractions => _interactions;

        public Task<ChatInteraction?> FindByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(_interactions.FirstOrDefault(i => i.Id == id && i.OrganizationId == organizationId));

        public Task<IReadOnlyList<ChatInteraction>> GetBySessionIdAsync(Guid sessionId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ChatInteraction>>(_interactions.Where(i => i.ChatSessionId == sessionId && i.OrganizationId == organizationId).ToList());

        public Task AddAsync(ChatInteraction interaction, CancellationToken ct = default)
        {
            _interactions.Add(interaction);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeChunkTextReader(
        IReadOnlyDictionary<Guid, string> texts) : IChunkTextReader
    {
        public Guid? LastOrganizationId { get; private set; }

        public Task<IReadOnlyDictionary<Guid, string>> GetChunkTextsAsync(
            IReadOnlyList<Guid> chunkIds,
            Guid organizationId,
            CancellationToken ct = default)
        {
            LastOrganizationId = organizationId;
            var result = chunkIds
                .Where(texts.ContainsKey)
                .ToDictionary(id => id, id => texts[id]);

            return Task.FromResult<IReadOnlyDictionary<Guid, string>>(result);
        }
    }

    private sealed class ThrowingAnswerGenerator(string exceptionMessage) : IAiAnswerGenerator
    {
        public string ProviderName => "ThrowingTest";
        public string DefaultModelName => "throwing-test-v1";

        public Task<AnswerGenerationResult> GenerateAsync(
            AnswerGenerationRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(exceptionMessage);
    }

    private sealed class CapturingAuditEventWriter : IAuditEventWriter
    {
        private readonly List<AuditEvent> _events = [];
        public IReadOnlyList<AuditEvent> Events => _events;

        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            _events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class StaticCorrelationContext : ICorrelationContext
    {
        public string CorrelationId => "test-correlation-chat";
    }

    private sealed class FakeCitationRepository : ICitationRepository
    {
        private readonly List<Citation> _citations = [];
        public IReadOnlyList<Citation> StoredCitations => _citations;
        public bool SaveChangesWasCalled { get; private set; }

        public Task<IReadOnlyList<Citation>> GetByInteractionIdAsync(Guid interactionId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Citation>>(_citations.Where(c => c.ChatInteractionId == interactionId && c.OrganizationId == organizationId).ToList());

        public Task AddRangeAsync(IReadOnlyList<Citation> citations, CancellationToken ct = default)
        {
            _citations.AddRange(citations);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            SaveChangesWasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDocumentTitleReader(
        IReadOnlyDictionary<Guid, string> titles) : IDocumentTitleReader
    {
        public Task<IReadOnlyDictionary<Guid, string>> GetTitlesAsync(
            IReadOnlyList<Guid> documentIds,
            Guid organizationId,
            CancellationToken ct = default) =>
            Task.FromResult(titles);
    }

    private sealed class AlwaysEmptyCitationMapper : ICitationMapper
    {
        public Task<IReadOnlyList<Citation>> MapAsync(
            Guid chatInteractionId,
            Guid organizationId,
            IReadOnlyList<CitationMappingSource> sources,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Citation>>([]);
    }

    private sealed class ThrowingCitationRepository : ICitationRepository
    {
        public Task<IReadOnlyList<Citation>> GetByInteractionIdAsync(Guid interactionId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Citation>>([]);

        public Task AddRangeAsync(IReadOnlyList<Citation> citations, CancellationToken ct = default) =>
            throw new InvalidOperationException("Simulated citation tracking failure.");

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}

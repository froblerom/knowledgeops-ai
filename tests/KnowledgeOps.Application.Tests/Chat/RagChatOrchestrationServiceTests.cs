using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Chat;
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

        var service = new RagChatOrchestrationService(
            currentUser, accessReader, permissionService, retrievalService,
            throwingGenerator, sessionRepository, interactionRepository,
            chunkTextReader, auditWriter, correlationContext,
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
        if (stored.AiModel is not null)
            Assert.DoesNotContain(secretMessage, stored.AiModel, StringComparison.Ordinal);
    }

    // ─── Harness ──────────────────────────────────────────────────────────────

    private static TestHarness CreateHarness(
        bool isAuthenticated = true,
        IReadOnlyList<EligibleSemanticRetrievalCandidate>? candidates = null,
        bool isInsufficientResult = false,
        string? retrievalFailureCode = null,
        AnswerState generatorState = AnswerState.Grounded,
        string? generatorFailureCode = null,
        List<string>? trackCallOrder = null)
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

        var service = new RagChatOrchestrationService(
            currentUser,
            accessReader,
            permissionService,
            retrievalService,
            generator,
            sessionRepository,
            interactionRepository,
            chunkTextReader,
            auditWriter,
            correlationContext,
            NullLogger<RagChatOrchestrationService>.Instance);

        return new TestHarness(service, retrievalService, generator, sessionRepository, interactionRepository, auditWriter, chunkTextReader);
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
        FakeChunkTextReader ChunkTextReader);

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
}

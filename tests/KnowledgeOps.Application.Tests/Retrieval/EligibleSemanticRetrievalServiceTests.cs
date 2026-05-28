using System.Security.Cryptography;
using System.Text;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Embeddings;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Application.Retrieval;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeOps.Application.Tests.Retrieval;

public sealed class EligibleSemanticRetrievalServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ActiveOrganizationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ClaimOrganizationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly RetrievalProviderMetadata ProviderMetadata =
        new("TestProvider", "TestAdapter", "TestStorage");

    [Theory]
    [MemberData(nameof(UnauthorizedCases))]
    public async Task RetrieveAsync_RejectsUnauthorizedRequestsWithoutCallingProviders(
        string expectedCode,
        bool isAuthenticated,
        UserAccessState? activeState,
        string queryText)
    {
        var harness = CreateHarness(
            isAuthenticated: isAuthenticated,
            activeState: activeState,
            useDefaultActiveState: false);

        var result = await harness.Service.RetrieveAsync(
            new EligibleSemanticRetrievalRequest(queryText, TopK: 3));

        Assert.Equal(expectedCode, result.FailureCode);
        Assert.Empty(result.Candidates);
        Assert.Equal(0, harness.EmbeddingProvider.GenerateCallCount);
        Assert.Equal(0, harness.SemanticProvider.SearchCallCount);
        Assert.Null(harness.Repository.LastOrganizationId);
    }

    [Fact]
    public void RetrievalContracts_DoNotExposeCallerOrRawQueryInputs()
    {
        var requestProperties = PublicPropertyNames<EligibleSemanticRetrievalRequest>();
        Assert.DoesNotContain("OrganizationId", requestProperties);
        Assert.DoesNotContain("UserId", requestProperties);
        Assert.DoesNotContain("QueryVector", requestProperties);

        var semanticRequestProperties = PublicPropertyNames<SemanticQueryRequest>();
        Assert.DoesNotContain("QueryText", semanticRequestProperties);
    }

    [Fact]
    public async Task RetrieveAsync_UsesActiveUserOrganizationInsteadOfCurrentUserClaims()
    {
        var candidate = Candidate(ActiveOrganizationId);
        var harness = CreateHarness(candidates: [candidate], revalidated: [Identity(candidate)]);

        var result = await harness.Service.RetrieveAsync(
            new EligibleSemanticRetrievalRequest("Where is the policy?", TopK: 1));

        var returned = Assert.Single(result.Candidates);
        Assert.Null(result.FailureCode);
        Assert.Equal(ActiveOrganizationId, harness.SemanticProvider.LastRequest!.OrganizationId);
        Assert.Equal(ActiveOrganizationId, harness.Repository.LastOrganizationId);
        Assert.Equal(ActiveOrganizationId, returned.OrganizationId);
        Assert.NotEqual(ClaimOrganizationId, harness.SemanticProvider.LastRequest.OrganizationId);
    }

    [Fact]
    public async Task RetrieveAsync_HashesTrimmedQueryAndDoesNotReturnRawQueryText()
    {
        const string rawQuery = "  Secret query text  ";
        var candidate = Candidate(ActiveOrganizationId);
        var harness = CreateHarness(candidates: [candidate], revalidated: [Identity(candidate)]);

        var result = await harness.Service.RetrieveAsync(
            new EligibleSemanticRetrievalRequest(rawQuery, TopK: 1));

        Assert.Equal(ComputeHash("Secret query text"), result.QueryHash);
        Assert.Equal(result.QueryHash.ToLowerInvariant(), result.QueryHash);
        Assert.DoesNotContain(rawQuery.Trim(), ResultStrings(result), StringComparer.Ordinal);
        Assert.DoesNotContain(rawQuery, ResultStrings(result), StringComparer.Ordinal);
    }

    [Fact]
    public async Task RetrieveAsync_CallsEmbeddingThenSemanticSearchWithQueryVectorOnly()
    {
        var candidate = Candidate(ActiveOrganizationId);
        var harness = CreateHarness(candidates: [candidate], revalidated: [Identity(candidate)]);

        await harness.Service.RetrieveAsync(
            new EligibleSemanticRetrievalRequest("What changed?", TopK: 2, MinimumScore: 0.75));

        Assert.Equal(1, harness.EmbeddingProvider.GenerateCallCount);
        Assert.Equal("What changed?", harness.EmbeddingProvider.LastRequest!.Text);
        Assert.Equal("test-query-model", harness.EmbeddingProvider.LastRequest.ModelName);
        Assert.Equal(3, harness.EmbeddingProvider.LastRequest.Dimensions);
        Assert.Equal(1, harness.SemanticProvider.SearchCallCount);
        Assert.Equal([0.25f, 0.5f, 0.75f], harness.SemanticProvider.LastRequest!.QueryVector);
        Assert.Equal(2, harness.SemanticProvider.LastRequest.TopK);
        Assert.Equal(0.75, harness.SemanticProvider.LastRequest.MinimumScore);
        Assert.DoesNotContain(
            typeof(SemanticQueryRequest).GetProperties(),
            property => property.Name.Contains("QueryText", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RetrieveAsync_ExcludesCrossOrganizationCandidatesBeforeRevalidation()
    {
        var sameOrg = Candidate(ActiveOrganizationId);
        var crossOrg = Candidate(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        var harness = CreateHarness(candidates: [crossOrg, sameOrg], revalidated: [Identity(sameOrg)]);

        var result = await harness.Service.RetrieveAsync(
            new EligibleSemanticRetrievalRequest("policy", TopK: 2));

        var revalidated = Assert.Single(harness.Repository.LastCandidates);
        Assert.Equal(sameOrg.ChunkEmbeddingId, revalidated.ChunkEmbeddingId);
        Assert.DoesNotContain(result.Candidates, candidate => candidate.OrganizationId == crossOrg.OrganizationId);
        Assert.Contains(
            harness.AuditWriter.Events,
            audit => audit.EventType == AuditEventTypes.StaleRetrievalCandidateExcluded);
    }

    [Fact]
    public async Task RetrieveAsync_HandlesStaleCandidatesAfterRepositoryRevalidation()
    {
        var eligible = Candidate(ActiveOrganizationId);
        var stale = Candidate(ActiveOrganizationId);
        var harness = CreateHarness(candidates: [stale, eligible], revalidated: [Identity(eligible)]);

        var result = await harness.Service.RetrieveAsync(
            new EligibleSemanticRetrievalRequest("policy", TopK: 2));

        var returned = Assert.Single(result.Candidates);
        Assert.Equal(eligible.ChunkEmbeddingId, returned.ChunkEmbeddingId);
        Assert.DoesNotContain(result.Candidates, candidate => candidate.ChunkEmbeddingId == stale.ChunkEmbeddingId);
        Assert.Contains(
            harness.AuditWriter.Events,
            audit => audit.EventType == AuditEventTypes.StaleRetrievalCandidateExcluded
                && audit.Message.Contains("eligibility revalidation", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsAuthorizedCandidatesWithRanksAssignedAfterFiltering()
    {
        var stale = Candidate(ActiveOrganizationId, score: 0.99);
        var firstEligible = Candidate(ActiveOrganizationId, score: 0.9);
        var secondEligible = Candidate(ActiveOrganizationId, score: 0.8);
        var harness = CreateHarness(
            candidates: [stale, firstEligible, secondEligible],
            revalidated: [Identity(firstEligible), Identity(secondEligible)]);

        var result = await harness.Service.RetrieveAsync(
            new EligibleSemanticRetrievalRequest("policy", TopK: 3));

        Assert.False(result.IsInsufficientResult);
        Assert.Equal(2, result.ReturnedCount);
        Assert.Collection(
            result.Candidates,
            candidate =>
            {
                Assert.Equal(1, candidate.Rank);
                Assert.Equal(firstEligible.ChunkEmbeddingId, candidate.ChunkEmbeddingId);
            },
            candidate =>
            {
                Assert.Equal(2, candidate.Rank);
                Assert.Equal(secondEligible.ChunkEmbeddingId, candidate.ChunkEmbeddingId);
            });
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsInsufficientResultWhenNoFinalEligibleChunksRemain()
    {
        var harness = CreateHarness(candidates: [Candidate(ActiveOrganizationId)], revalidated: []);

        var result = await harness.Service.RetrieveAsync(
            new EligibleSemanticRetrievalRequest("policy", TopK: 1));

        Assert.True(result.IsInsufficientResult);
        Assert.Empty(result.Candidates);
        Assert.Equal(0, result.ReturnedCount);
        Assert.Contains(
            harness.AuditWriter.Events,
            audit => audit.EventType == AuditEventTypes.RetrievalInsufficientResults);
    }

    [Fact]
    public async Task RetrieveAsync_EmbeddingFailureReturnsSafeFailureWithoutLeakingSensitiveText()
    {
        const string rawQuery = "private payroll query";
        const string providerSecret = "raw-vector=[9.9,8.8]";
        var harness = CreateHarness();
        harness.EmbeddingProvider.Exception = new InvalidOperationException(providerSecret);

        var result = await harness.Service.RetrieveAsync(
            new EligibleSemanticRetrievalRequest(rawQuery, TopK: 1));

        Assert.Equal("QueryEmbeddingFailed", result.FailureCode);
        Assert.Equal("Query embedding generation failed.", result.FailureReason);
        Assert.Empty(result.Candidates);
        Assert.Equal(0, harness.SemanticProvider.SearchCallCount);
        AssertNoSensitiveText(result, harness.AuditWriter.Events, rawQuery, providerSecret);
    }

    [Fact]
    public async Task RetrieveAsync_SemanticProviderFailureReturnsSafeFailureWithoutLeakingSensitiveText()
    {
        const string rawQuery = "private merger query";
        const string providerSecret = "chunk text and vector [1,2,3]";
        var harness = CreateHarness();
        harness.SemanticProvider.Exception = new InvalidOperationException(providerSecret);

        var result = await harness.Service.RetrieveAsync(
            new EligibleSemanticRetrievalRequest(rawQuery, TopK: 1));

        Assert.Equal("SemanticSearchFailed", result.FailureCode);
        Assert.Equal("Semantic retrieval failed.", result.FailureReason);
        Assert.Empty(result.Candidates);
        AssertNoSensitiveText(result, harness.AuditWriter.Events, rawQuery, providerSecret);
    }

    [Fact]
    public void EligibleCandidateContract_DoesNotExposeRawTextOrVectors()
    {
        var sensitiveNameFragments = new[] { "Text", "Query", "Vector", "Prompt", "Answer" };

        var properties = typeof(EligibleSemanticRetrievalCandidate).GetProperties();

        Assert.DoesNotContain(
            properties,
            property => sensitiveNameFragments.Any(fragment =>
                property.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void EligibleRetrievalService_DoesNotDependOnPromptOrAnswerGenerationContracts()
    {
        var constructorParameterNames = typeof(EligibleSemanticRetrievalService)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(parameter => parameter.ParameterType.Name)
            .ToArray();

        Assert.DoesNotContain(
            constructorParameterNames,
            name => name.Contains("Chat", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Prompt", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Answer", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Generation", StringComparison.OrdinalIgnoreCase));
    }

    public static TheoryData<string, bool, UserAccessState?, string> UnauthorizedCases() =>
        new()
        {
            { "Unauthenticated", false, ActiveState(), "policy" },
            { "UserInactive", true, null, "policy" },
            { "PermissionDenied", true, ActiveState("UnknownRole"), "policy" },
            { "InvalidOrganization", true, ActiveState(organizationId: Guid.Empty), "policy" },
            { "InvalidQuery", true, ActiveState(), "   " }
        };

    private static TestHarness CreateHarness(
        bool isAuthenticated = true,
        UserAccessState? activeState = null,
        bool useDefaultActiveState = true,
        IReadOnlyList<RetrievedChunkCandidate>? candidates = null,
        IReadOnlyList<RetrievalEligibleCandidateIdentity>? revalidated = null)
    {
        var currentUser = new FakeCurrentUser(isAuthenticated, UserId, ClaimOrganizationId);
        var accessReader = new FakeAccessStateReader(useDefaultActiveState ? activeState ?? ActiveState() : activeState);
        var permissionService = new PermissionService();
        var embeddingProvider = new FakeEmbeddingProvider();
        var semanticProvider = new FakeSemanticSearchProvider(candidates ?? []);
        var repository = new FakeRetrievalEligibilityRepository(revalidated ?? []);
        var auditWriter = new CapturingAuditEventWriter();
        var service = new EligibleSemanticRetrievalService(
            currentUser,
            accessReader,
            permissionService,
            embeddingProvider,
            semanticProvider,
            repository,
            auditWriter,
            new StaticCorrelationContext(),
            NullLogger<EligibleSemanticRetrievalService>.Instance);

        return new TestHarness(
            service,
            embeddingProvider,
            semanticProvider,
            repository,
            auditWriter);
    }

    private static UserAccessState ActiveState(
        string role = "Agent",
        Guid? organizationId = null) =>
        new(UserId, organizationId ?? ActiveOrganizationId, [role]);

    private static RetrievedChunkCandidate Candidate(Guid organizationId, double score = 0.9) =>
        new(
            organizationId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new RetrievalScore(score, "CosineSimilarity"),
            "CosineSimilarity",
            "Provider",
            "model",
            ChunkIndex: 1,
            PageNumber: 2,
            SectionLabel: "Policy");

    private static RetrievalEligibleCandidateIdentity Identity(RetrievedChunkCandidate candidate) =>
        new(
            candidate.OrganizationId,
            candidate.DocumentId,
            candidate.ChunkId,
            candidate.ChunkEmbeddingId);

    private static string[] PublicPropertyNames<T>() =>
        typeof(T).GetProperties().Select(property => property.Name).ToArray();

    private static string ComputeHash(string queryText)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(queryText));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static IEnumerable<string> ResultStrings(EligibleSemanticRetrievalResult result)
    {
        yield return result.QueryHash;
        if (result.FailureCode is not null)
            yield return result.FailureCode;
        if (result.FailureReason is not null)
            yield return result.FailureReason;

        foreach (var candidate in result.Candidates)
        {
            yield return candidate.ScoreMethod;
            yield return candidate.ProviderName;
            yield return candidate.ModelName;
            if (candidate.SectionLabel is not null)
                yield return candidate.SectionLabel;
        }
    }

    private static void AssertNoSensitiveText(
        EligibleSemanticRetrievalResult result,
        IReadOnlyList<AuditEvent> auditEvents,
        params string[] sensitiveValues)
    {
        var inspected = ResultStrings(result)
            .Concat(auditEvents.Select(audit => audit.Message))
            .ToArray();

        foreach (var sensitiveValue in sensitiveValues)
        {
            Assert.DoesNotContain(
                inspected,
                value => value.Contains(sensitiveValue, StringComparison.Ordinal));
        }
    }

    private sealed record TestHarness(
        EligibleSemanticRetrievalService Service,
        FakeEmbeddingProvider EmbeddingProvider,
        FakeSemanticSearchProvider SemanticProvider,
        FakeRetrievalEligibilityRepository Repository,
        CapturingAuditEventWriter AuditWriter);

    private sealed class FakeCurrentUser(
        bool isAuthenticated,
        Guid userId,
        Guid organizationId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
        public Guid OrganizationId { get; } = organizationId;
        public string Email => "agent@example.test";
        public string DisplayName => "Agent";
        public IReadOnlyList<string> Roles => ["Admin"];
        public bool IsAuthenticated { get; } = isAuthenticated;
    }

    private sealed class FakeAccessStateReader(UserAccessState? activeState) : IUserAccessStateReader
    {
        public Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(activeState);
    }

    private sealed class FakeEmbeddingProvider : IEmbeddingProvider
    {
        public string ProviderName => "TestEmbedding";
        public string DefaultModelName => "test-query-model";
        public int DefaultDimensions => 3;
        public EmbeddingRequest? LastRequest { get; private set; }
        public int GenerateCallCount { get; private set; }
        public Exception? Exception { get; set; }

        public Task<EmbeddingResponse> GenerateAsync(
            EmbeddingRequest request,
            CancellationToken cancellationToken = default)
        {
            GenerateCallCount++;
            LastRequest = request;

            if (Exception is not null)
                throw Exception;

            return Task.FromResult(new EmbeddingResponse([0.25f, 0.5f, 0.75f]));
        }
    }

    private sealed class FakeSemanticSearchProvider(
        IReadOnlyList<RetrievedChunkCandidate> candidates) : ISemanticSearchProvider
    {
        public SemanticQueryRequest? LastRequest { get; private set; }
        public int SearchCallCount { get; private set; }
        public Exception? Exception { get; set; }

        public Task<SemanticQueryResult> SearchAsync(
            SemanticQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            SearchCallCount++;
            LastRequest = request;

            if (Exception is not null)
                throw Exception;

            return Task.FromResult(new SemanticQueryResult(
                candidates,
                "CosineSimilarity",
                request.TopK,
                request.TopK,
                candidates.Count,
                0,
                0,
                0,
                ProviderMetadata));
        }
    }

    private sealed class FakeRetrievalEligibilityRepository(
        IReadOnlyList<RetrievalEligibleCandidateIdentity> revalidated) : IRetrievalEligibilityRepository
    {
        public Guid? LastOrganizationId { get; private set; }
        public IReadOnlyList<RetrievalCandidateIdentity> LastCandidates { get; private set; } = [];

        public Task<IReadOnlyList<RetrievalEligibleCandidateIdentity>> RevalidateAsync(
            Guid organizationId,
            IReadOnlyList<RetrievalCandidateIdentity> candidates,
            CancellationToken cancellationToken = default)
        {
            LastOrganizationId = organizationId;
            LastCandidates = candidates;
            return Task.FromResult(revalidated);
        }
    }

    private sealed class CapturingAuditEventWriter : IAuditEventWriter
    {
        private readonly List<AuditEvent> events = [];

        public IReadOnlyList<AuditEvent> Events => events;

        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class StaticCorrelationContext : ICorrelationContext
    {
        public string CorrelationId => "test-correlation";
    }
}

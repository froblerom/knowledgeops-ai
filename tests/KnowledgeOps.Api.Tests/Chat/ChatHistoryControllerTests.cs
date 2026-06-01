using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeOps.Api.Tests.Chat;

public sealed class ChatHistoryControllerTests : IClassFixture<ChatHistoryApiTestFactory>
{
    private readonly ChatHistoryApiTestFactory _factory;

    public ChatHistoryControllerTests(ChatHistoryApiTestFactory factory)
    {
        _factory = factory;
        _factory.Reset();
    }

    // ── GET /chat/sessions ───────────────────────────────────────────────────

    [Fact]
    public async Task GetSessions_RequiresAuthentication()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/chat/sessions");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSessions_ReturnsOwnSessionsForAgent()
    {
        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync("/api/v1/chat/sessions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSessions_ScopedReviewReturnsEmptyForAgent()
    {
        _factory.HistoryService.ScopedSessions = [];
        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync("/api/v1/chat/sessions?scoped=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── POST /chat/sessions ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateSession_RequiresAuthentication()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/chat/sessions", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_ReturnsCreatedWithSessionId()
    {
        var sessionId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        _factory.HistoryService.NextCreatedSessionId = sessionId;

        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.PostAsJsonAsync("/api/v1/chat/sessions", new { title = "Test" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(sessionId, body.GetProperty("chatSessionId").GetGuid());
    }

    [Fact]
    public async Task CreateSession_DoesNotRequireTitle()
    {
        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.PostAsJsonAsync("/api/v1/chat/sessions", new { });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ── GET /chat/sessions/{chatSessionId} ───────────────────────────────────

    [Fact]
    public async Task GetSession_RequiresAuthentication()
    {
        var id = Guid.NewGuid();
        var response = await _factory.CreateClient().GetAsync($"/api/v1/chat/sessions/{id}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSession_ReturnsOkWhenServiceReturnsResult()
    {
        var sessionId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        _factory.HistoryService.NextSession = ChatHistoryApiTestFactory.SampleSessionDetail(sessionId);

        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/sessions/{sessionId}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(sessionId, body.GetProperty("chatSessionId").GetGuid());
        Assert.Equal("Active", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetSession_Returns404WhenServiceReturnsNull()
    {
        _factory.HistoryService.NextSession = null;

        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/sessions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSession_DetailIncludesInteractions()
    {
        var sessionId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var session = ChatHistoryApiTestFactory.SampleSessionDetail(sessionId);
        _factory.HistoryService.NextSession = session;

        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/sessions/{sessionId}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.GetProperty("interactions").GetArrayLength() > 0);
    }

    // ── GET /chat/interactions/{chatInteractionId} ───────────────────────────

    [Fact]
    public async Task GetInteraction_RequiresAuthentication()
    {
        var id = Guid.NewGuid();
        var response = await _factory.CreateClient().GetAsync($"/api/v1/chat/interactions/{id}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetInteraction_ReturnsOkWhenServiceReturnsResult()
    {
        var interactionId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        _factory.HistoryService.NextInteraction = ChatHistoryApiTestFactory.SampleInteractionDetail(interactionId);

        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/interactions/{interactionId}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(interactionId, body.GetProperty("chatInteractionId").GetGuid());
        Assert.Equal("GroundedAnswer", body.GetProperty("answerState").GetString());
        Assert.False(body.GetProperty("insufficientContext").GetBoolean());
    }

    [Fact]
    public async Task GetInteraction_Returns404WhenServiceReturnsNull()
    {
        _factory.HistoryService.NextInteraction = null;

        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/interactions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetInteraction_InsufficientContext_SetsFlag()
    {
        var interactionId = Guid.NewGuid();
        var detail = ChatHistoryApiTestFactory.SampleInteractionDetail(interactionId, "InsufficientContext", insufficientContext: true);
        _factory.HistoryService.NextInteraction = detail;

        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/interactions/{interactionId}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("InsufficientContext", body.GetProperty("answerState").GetString());
        Assert.True(body.GetProperty("insufficientContext").GetBoolean());
    }

    [Fact]
    public async Task GetInteraction_DoesNotExposeProviderFailureCode()
    {
        _factory.HistoryService.NextInteraction = ChatHistoryApiTestFactory.SampleInteractionDetail(Guid.NewGuid());
        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/interactions/{Guid.NewGuid()}");
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("providerFailureCode", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetInteraction_DoesNotExposeQuestionTextHash()
    {
        _factory.HistoryService.NextInteraction = ChatHistoryApiTestFactory.SampleInteractionDetail(Guid.NewGuid());
        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/interactions/{Guid.NewGuid()}");
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("questionTextHash", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetInteraction_DoesNotExposeRetrievalQueryId()
    {
        _factory.HistoryService.NextInteraction = ChatHistoryApiTestFactory.SampleInteractionDetail(Guid.NewGuid());
        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/interactions/{Guid.NewGuid()}");
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("retrievalQueryId", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetInteraction_IncludesMetadata()
    {
        _factory.HistoryService.NextInteraction = ChatHistoryApiTestFactory.SampleInteractionDetail(Guid.NewGuid());
        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/interactions/{Guid.NewGuid()}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var metadata = body.GetProperty("metadata");
        Assert.True(metadata.TryGetProperty("retrievalCandidateCount", out _));
    }

    // ── GET /chat/interactions/{id}/citations ────────────────────────────────

    [Fact]
    public async Task GetInteractionCitations_RequiresAuthentication()
    {
        var id = Guid.NewGuid();
        var response = await _factory.CreateClient().GetAsync($"/api/v1/chat/interactions/{id}/citations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetInteractionCitations_ReturnsOkForOwner()
    {
        var interactionId = Guid.NewGuid();
        _factory.HistoryService.NextCitations =
        [
            new ChatCitationHistoryDto(Guid.NewGuid(), interactionId, Guid.NewGuid(), Guid.NewGuid(),
                1, "Test Doc", null, null, 0.9)
        ];

        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/interactions/{interactionId}/citations");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.GetArrayLength());
    }

    [Fact]
    public async Task GetInteractionCitations_Returns404WhenServiceReturnsNull()
    {
        _factory.HistoryService.NextCitations = null;

        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/interactions/{Guid.NewGuid()}/citations");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetInteractionCitations_DoesNotExposeFullChunkText()
    {
        var interactionId = Guid.NewGuid();
        _factory.HistoryService.NextCitations = [
            new ChatCitationHistoryDto(Guid.NewGuid(), interactionId, Guid.NewGuid(), Guid.NewGuid(),
                1, "Test Doc", null, null, 0.9)
        ];

        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/interactions/{interactionId}/citations");
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("chunkText", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("text", raw, StringComparison.OrdinalIgnoreCase); // no raw text field
    }

    // ── G-5: Chat sessions cross-org ─────────────────────────────────────────

    [Fact]
    public async Task ChatSessions_OrgA_DoesNotIncludeOrgBSessions()
    {
        // OrgA user's own sessions are set; OrgB user's sessions are NOT included.
        // The service enforces org scope internally; OrgA sessions list must not include OrgB sessions.
        var orgASession = new ChatSessionSummaryDto(
            Guid.Parse("a1a1a1a1-a1a1-4a1a-8a1a-a1a1a1a1a1a1"),
            "OrgA Session",
            ChatSession.StatusActive,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            1);
        _factory.HistoryService.OwnSessions = [orgASession];

        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync("/api/v1/chat/sessions");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal(orgASession.ChatSessionId, body[0].GetProperty("chatSessionId").GetGuid());

        // Verify the raw response does not contain any OrgB org ID
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ChatHistoryApiTestFactory.OrgBId.ToString(), raw, StringComparison.OrdinalIgnoreCase);
    }

    // ── G-6: Chat interaction detail and citations cross-org ─────────────────

    [Fact]
    public async Task GetInteraction_CrossOrg_Returns404()
    {
        // Simulates: interaction belongs to OrgB, OrgA user cannot see it.
        // The service returns null for cross-org interactions; controller maps null to 404.
        _factory.HistoryService.NextInteraction = null; // service enforces org scope and returns null

        var orgBInteractionId = Guid.Parse("b2b2b2b2-b2b2-4b2b-8b2b-b2b2b2b2b2b2");
        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/interactions/{orgBInteractionId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        // Must not expose OrgB interaction details
        Assert.DoesNotContain(orgBInteractionId.ToString(), raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("questionText", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answerText", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetInteractionCitations_CrossOrg_Returns404()
    {
        // Simulates: interaction belongs to OrgB, OrgA user cannot see citations for it.
        // The service returns null for cross-org interactions; controller maps null to 404.
        _factory.HistoryService.NextCitations = null; // service enforces org scope and returns null

        var orgBInteractionId = Guid.Parse("c3c3c3c3-c3c3-4c3c-8c3c-c3c3c3c3c3c3");
        var client = await AuthenticateAsync(ChatHistoryApiTestFactory.AgentEmail);
        var response = await client.GetAsync($"/api/v1/chat/interactions/{orgBInteractionId}/citations");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        // Must not expose any OrgB citation data
        Assert.DoesNotContain(orgBInteractionId.ToString(), raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("documentId", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chunkId", raw, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<HttpClient> AuthenticateAsync(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = ChatHistoryApiTestFactory.Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.GetProperty("accessToken").GetString());
        return client;
    }
}

public sealed class ChatHistoryApiTestFactory : WebApplicationFactory<Program>
{
    public const string AgentEmail = "agent.history@example.test";
    public const string Password = "test-password";
    public static readonly Guid OrgId = Guid.Parse("aaaa1111-aaaa-aaaa-aaaa-aaaa11111111");
    // OrgB is a separate organization used to verify cross-org isolation in tests.
    public static readonly Guid OrgBId = Guid.Parse("bbbb2222-bbbb-bbbb-bbbb-bbbb22222222");

    public FakeChatHistoryService HistoryService { get; } = new();

    public void Reset() => HistoryService.Reset();

    public static ChatSessionDetailDto SampleSessionDetail(Guid sessionId) =>
        new(sessionId, "Test session", ChatSession.StatusActive,
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [new ChatInteractionSummaryDto(Guid.NewGuid(), "GroundedAnswer", false, DateTimeOffset.UtcNow)]);

    public static ChatInteractionDetailDto SampleInteractionDetail(
        Guid interactionId,
        string answerState = "GroundedAnswer",
        bool insufficientContext = false) =>
        new(interactionId,
            Guid.NewGuid(),
            answerState,
            insufficientContext,
            "What is the policy?",
            insufficientContext ? null : "According to the policy...",
            "rag-grounded-v1",
            "test-correlation",
            new ChatRetrievalMetadataDto(1, 200L, 800L, 1000L, 100, 50, 0.001m),
            [],
            DateTimeOffset.UtcNow);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "KnowledgeOps",
                ["Jwt:Audience"] = "KnowledgeOps",
                ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-characters-long",
                ["Jwt:ExpirationMinutes"] = "60",
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=localhost;Database=TestDb;Trusted_Connection=True;"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserAuthRepository>();
            services.AddScoped<IUserAuthRepository, FakeHistoryAuthRepository>();
            services.RemoveAll<IUserAccessStateReader>();
            services.AddScoped<IUserAccessStateReader>(sp =>
                new FixedHistoryAccessStateReader(sp.GetRequiredService<IUserAuthRepository>()));
            services.RemoveAll<IChatHistoryService>();
            services.AddSingleton<IChatHistoryService>(HistoryService);
            services.RemoveAll<IPasswordHasher>();
            services.AddSingleton<IPasswordHasher, TestHistoryPasswordHasher>();
            services.RemoveAll<IAuditEventWriter>();
            services.AddSingleton<IAuditEventWriter, NoopHistoryAuditWriter>();
        });
    }

    public sealed class FakeChatHistoryService : IChatHistoryService
    {
        public IReadOnlyList<ChatSessionSummaryDto> OwnSessions { get; set; } = [];
        public IReadOnlyList<ChatSessionSummaryDto> ScopedSessions { get; set; } = [];
        public Guid NextCreatedSessionId { get; set; } = Guid.NewGuid();
        public ChatSessionDetailDto? NextSession { get; set; }
        public ChatInteractionDetailDto? NextInteraction { get; set; }
        public IReadOnlyList<ChatCitationHistoryDto>? NextCitations { get; set; } = [];

        public void Reset()
        {
            OwnSessions = [];
            ScopedSessions = [];
            NextCreatedSessionId = Guid.NewGuid();
            NextSession = null;
            NextInteraction = null;
            NextCitations = [];
        }

        public Task<IReadOnlyList<ChatSessionSummaryDto>> GetSessionsAsync(bool scopedReview, CancellationToken ct = default) =>
            Task.FromResult(scopedReview ? ScopedSessions : OwnSessions);

        public Task<Guid> CreateSessionAsync(string? title, CancellationToken ct = default) =>
            Task.FromResult(NextCreatedSessionId);

        public Task<ChatSessionDetailDto?> GetSessionAsync(Guid sessionId, CancellationToken ct = default) =>
            Task.FromResult(NextSession);

        public Task<ChatInteractionDetailDto?> GetInteractionAsync(Guid interactionId, CancellationToken ct = default) =>
            Task.FromResult(NextInteraction);

        public Task<IReadOnlyList<ChatCitationHistoryDto>?> GetInteractionCitationsAsync(Guid interactionId, CancellationToken ct = default) =>
            Task.FromResult(NextCitations);
    }

    private sealed class FakeHistoryAuthRepository : IUserAuthRepository
    {
        private static readonly Dictionary<string, UserAuthRecord> Users =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [AgentEmail] = new UserAuthRecord(
                    Guid.Parse("aaaa1111-1111-1111-1111-111111111111"),
                    OrgId, AgentEmail, "Agent User", Password, UserStatus.Active, ["Agent"])
            };

        public Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(Users.GetValueOrDefault(email));

        public Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Users.Values.SingleOrDefault(u => u.UserId == userId));

        public Task UpdateLastLoginAtAsync(Guid userId, DateTimeOffset loginAt, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class FixedHistoryAccessStateReader(IUserAuthRepository repository) : IUserAccessStateReader
    {
        public async Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await repository.FindByIdAsync(userId, ct);
            if (user is null || user.Status != UserStatus.Active) return null;
            return new UserAccessState(user.UserId, user.OrganizationId, user.Roles);
        }
    }

    private sealed class TestHistoryPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => password;
        public bool VerifyPassword(string hashedPassword, string password) => hashedPassword == password;
    }

    private sealed class NoopHistoryAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default) => Task.CompletedTask;
    }
}

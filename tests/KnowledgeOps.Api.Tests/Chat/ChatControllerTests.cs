using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Application.Chat.Feedback;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeOps.Api.Tests.Chat;

public sealed class ChatControllerTests : IClassFixture<ChatApiTestFactory>
{
    private readonly ChatApiTestFactory _factory;

    public ChatControllerTests(ChatApiTestFactory factory)
    {
        _factory = factory;
        _factory.Reset();
    }

    [Fact]
    public async Task ChatQuestions_RequiresAuthentication()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { questionText = "What is the policy?" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(ChatApiTestFactory.AgentEmail)]
    [InlineData(ChatApiTestFactory.SupervisorEmail)]
    [InlineData(ChatApiTestFactory.KnowledgeAdminEmail)]
    [InlineData(ChatApiTestFactory.ManagerEmail)]
    [InlineData(ChatApiTestFactory.AdminEmail)]
    public async Task ChatQuestions_AllowsAllFiveMvpRoles(string email)
    {
        var client = await AuthenticateAsync(email);

        var response = await client.PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { questionText = "What is the policy?" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChatQuestions_RequiresAskQuestionPermission()
    {
        var client = await AuthenticateAsync(ChatApiTestFactory.NoRoleEmail);

        var response = await client.PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { questionText = "What is the policy?" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(_factory.RagService.WasCalled);
    }

    [Fact]
    public async Task ChatQuestions_RejectsEmptyQuestion()
    {
        var response = await (await AuthenticateAsync(ChatApiTestFactory.AgentEmail)).PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { questionText = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(_factory.RagService.WasCalled);
    }

    [Fact]
    public async Task ChatQuestions_PreservesChatSessionContinuity()
    {
        var sessionId = Guid.Parse("33333333-3333-4333-8333-333333333333");
        var client = await AuthenticateAsync(ChatApiTestFactory.AgentEmail);

        var response = await client.PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { chatSessionId = sessionId, questionText = "  What changed?  " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_factory.RagService.WasCalled);
        Assert.Equal(sessionId, _factory.RagService.LastRequest?.ChatSessionId);
        Assert.Equal("What changed?", _factory.RagService.LastRequest?.QuestionText);
    }

    [Fact]
    public async Task ChatQuestions_ReturnsGroundedAnswerWithCitations()
    {
        _factory.RagService.NextResponse = ChatApiTestFactory.GroundedResponse();
        var client = await AuthenticateAsync(ChatApiTestFactory.AgentEmail);

        var response = await client.PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { questionText = "What is the policy?" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("GroundedAnswer", body.GetProperty("answerState").GetString());
        Assert.Equal("Use the documented process.", body.GetProperty("answer").GetString());
        Assert.False(body.GetProperty("insufficientContext").GetBoolean());

        var citation = body.GetProperty("citations")[0];
        Assert.Equal(ChatApiTestFactory.CitationId, citation.GetProperty("citationId").GetGuid());
        Assert.Equal(ChatApiTestFactory.DocumentId, citation.GetProperty("documentId").GetGuid());
        Assert.Equal(ChatApiTestFactory.ChunkId, citation.GetProperty("chunkId").GetGuid());
        Assert.Equal(1, citation.GetProperty("rank").GetInt32());
        Assert.Equal("Refund Escalation Policy", citation.GetProperty("documentTitle").GetString());
        Assert.Equal(2, citation.GetProperty("pageNumber").GetInt32());
        Assert.Equal("Escalation Procedure", citation.GetProperty("sectionLabel").GetString());
        Assert.Equal(0.91, citation.GetProperty("score").GetDouble(), precision: 2);
    }

    [Fact]
    public async Task ChatQuestions_ReturnsInsufficientContext()
    {
        _factory.RagService.NextResponse = new AskQuestionResponse(
            ChatInteractionId: Guid.Parse("44444444-4444-4444-8444-444444444444"),
            ChatSessionId: Guid.Parse("55555555-5555-4555-8555-555555555555"),
            AnswerState.InsufficientContext,
            AnswerText: "The approved documents do not contain enough information to answer this question safely.",
            RetrievalCandidateCount: 0,
            IsInsufficientContext: true,
            CorrelationId: "insufficient-correlation",
            Citations: null);

        var response = await (await AuthenticateAsync(ChatApiTestFactory.AgentEmail)).PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { questionText = "What is the unpublished policy?" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("InsufficientContext", body.GetProperty("answerState").GetString());
        Assert.True(body.GetProperty("insufficientContext").GetBoolean());
        Assert.Empty(body.GetProperty("citations").EnumerateArray());
    }

    [Fact]
    public async Task ChatQuestions_ReturnsProviderFailureSafely()
    {
        _factory.RagService.NextResponse = ChatApiTestFactory.ProviderFailureResponse();

        var response = await (await AuthenticateAsync(ChatApiTestFactory.AgentEmail)).PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { questionText = "What is the policy?" });
        var raw = await response.Content.ReadAsStringAsync();
        var body = JsonDocument.Parse(raw).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ProviderFailure", body.GetProperty("answerState").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("answer").ValueKind);
        Assert.Empty(body.GetProperty("citations").EnumerateArray());
        Assert.DoesNotContain("raw provider failure detail", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("providerFailureCode", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChatQuestions_DoesNotExposeExceptionMessages()
    {
        _factory.RagService.ExceptionToThrow =
            new InvalidOperationException("ConnectionString=Server=internal;Password=secret123");

        var response = await (await AuthenticateAsync(ChatApiTestFactory.AgentEmail)).PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { questionText = "What is the policy?" });
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("ConnectionString", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret123", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChatQuestions_AnswerStateSerializesAsGroundedAnswer()
    {
        _factory.RagService.NextResponse = ChatApiTestFactory.GroundedResponse();

        var response = await (await AuthenticateAsync(ChatApiTestFactory.AgentEmail)).PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { questionText = "What is the policy?" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("GroundedAnswer", body.GetProperty("answerState").GetString());
        Assert.NotEqual(AnswerState.Grounded.ToString(), body.GetProperty("answerState").GetString());
    }

    [Fact]
    public async Task ChatQuestions_ProviderFailedMapsToProviderFailure()
    {
        _factory.RagService.NextResponse = ChatApiTestFactory.ProviderFailureResponse();

        var response = await (await AuthenticateAsync(ChatApiTestFactory.AgentEmail)).PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { questionText = "What is the policy?" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("ProviderFailure", body.GetProperty("answerState").GetString());
    }

    [Fact]
    public async Task ChatQuestions_DoesNotAcceptOrganizationIdFromRequest()
    {
        var client = await AuthenticateAsync(ChatApiTestFactory.AgentEmail);

        var response = await client.PostAsJsonAsync(
            "/api/v1/chat/questions",
            new
            {
                questionText = "What is the policy?",
                organizationId = Guid.Parse("99999999-9999-4999-8999-999999999999")
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_factory.RagService.WasCalled);
        Assert.Equal("What is the policy?", _factory.RagService.LastRequest?.QuestionText);
    }

    [Fact]
    public async Task ChatQuestions_ReturnsNullableEstimatedCostWhenUnavailable()
    {
        var response = await (await AuthenticateAsync(ChatApiTestFactory.AgentEmail)).PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { questionText = "What is the policy?" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Null, body.GetProperty("metadata").GetProperty("estimatedCost").ValueKind);
        Assert.Equal(1, body.GetProperty("metadata").GetProperty("retrievalResultCount").GetInt32());
    }

    private async Task<HttpClient> AuthenticateAsync(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = ChatApiTestFactory.Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.GetProperty("accessToken").GetString());
        return client;
    }
}

public sealed class ChatApiTestFactory : WebApplicationFactory<Program>
{
    public const string AgentEmail = "agent@example.test";
    public const string SupervisorEmail = "supervisor@example.test";
    public const string KnowledgeAdminEmail = "knowledgeadmin@example.test";
    public const string ManagerEmail = "manager@example.test";
    public const string AdminEmail = "admin@example.test";
    public const string NoRoleEmail = "norole@example.test";
    public const string Password = "test-password";

    public static readonly Guid OrgId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid CitationId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    public static readonly Guid DocumentId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    public static readonly Guid ChunkId = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");

    public FakeRagChatOrchestrationService RagService { get; } = new();
    public FakeAnswerFeedbackService FeedbackService { get; } = new();

    public void Reset()
    {
        RagService.Reset();
        FeedbackService.Reset();
    }

    public static AskQuestionResponse GroundedResponse() =>
        new(
            ChatInteractionId: Guid.Parse("11111111-2222-4333-8444-555555555555"),
            ChatSessionId: Guid.Parse("22222222-3333-4444-8555-666666666666"),
            AnswerState.Grounded,
            AnswerText: "Use the documented process.",
            RetrievalCandidateCount: 1,
            IsInsufficientContext: false,
            CorrelationId: "chat-test-correlation",
            Citations:
            [
                new CitationResponse(
                    CitationId,
                    DocumentId,
                    ChunkId,
                    Rank: 1,
                    DocumentTitle: "Refund Escalation Policy",
                    PageNumber: 2,
                    SectionLabel: "Escalation Procedure",
                    RelevanceScore: 0.91)
            ]);

    public static AskQuestionResponse ProviderFailureResponse() =>
        new(
            ChatInteractionId: Guid.Parse("44444444-4444-4444-8444-444444444444"),
            ChatSessionId: Guid.Parse("55555555-5555-4555-8555-555555555555"),
            AnswerState.ProviderFailed,
            AnswerText: "raw provider failure detail",
            RetrievalCandidateCount: 0,
            IsInsufficientContext: false,
            CorrelationId: "provider-failed-correlation",
            Citations: null);

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
            services.AddScoped<IUserAuthRepository, FakeAuthRepository>();
            services.RemoveAll<IUserAccessStateReader>();
            services.AddScoped<IUserAccessStateReader>(sp =>
                new FixedAccessStateReader(sp.GetRequiredService<IUserAuthRepository>()));
            services.RemoveAll<IRagChatOrchestrationService>();
            services.AddSingleton<IRagChatOrchestrationService>(RagService);
            services.RemoveAll<IAnswerFeedbackService>();
            services.AddSingleton<IAnswerFeedbackService>(FeedbackService);
            services.RemoveAll<IPasswordHasher>();
            services.AddSingleton<IPasswordHasher, TestPasswordHasher>();
            services.RemoveAll<IAuditEventWriter>();
            services.AddSingleton<IAuditEventWriter, NoopAuditEventWriter>();
        });
    }

    public sealed class FakeAnswerFeedbackService : IAnswerFeedbackService
    {
        public bool SubmitWasCalled { get; private set; }
        public bool UpdateWasCalled { get; private set; }
        public bool ReviewWasCalled { get; private set; }
        public SubmitAnswerFeedbackRequest? LastSubmitRequest { get; private set; }
        public UpdateAnswerFeedbackRequest? LastUpdateRequest { get; private set; }
        public Exception? ExceptionToThrow { get; set; }
        public AnswerFeedbackResult NextFeedbackResult { get; set; } =
            FeedbackResult(Guid.Parse("11111111-1111-4111-8111-111111111111"), AnswerFeedbackRating.Useful);
        public AnswerFeedbackReviewResult NextReviewResult { get; set; } =
            new(UsefulCount: 1, NotUsefulCount: 1, Items:
            [
                new AnswerFeedbackReviewItem(
                    Guid.Parse("11111111-1111-4111-8111-111111111111"),
                    Guid.Parse("22222222-2222-4222-8222-222222222222"),
                    Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                    AnswerFeedbackRating.Useful,
                    DateTimeOffset.Parse("2026-05-30T00:00:00Z"),
                    DateTimeOffset.Parse("2026-05-30T00:00:00Z"))
            ]);

        public void Reset()
        {
            SubmitWasCalled = false;
            UpdateWasCalled = false;
            ReviewWasCalled = false;
            LastSubmitRequest = null;
            LastUpdateRequest = null;
            ExceptionToThrow = null;
            NextFeedbackResult = FeedbackResult(
                Guid.Parse("11111111-1111-4111-8111-111111111111"),
                AnswerFeedbackRating.Useful);
        }

        public Task<AnswerFeedbackResult> SubmitAsync(
            SubmitAnswerFeedbackRequest request,
            CancellationToken ct = default)
        {
            SubmitWasCalled = true;
            LastSubmitRequest = request;
            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;
            return Task.FromResult(NextFeedbackResult);
        }

        public Task<AnswerFeedbackResult> UpdateOwnAsync(
            UpdateAnswerFeedbackRequest request,
            CancellationToken ct = default)
        {
            UpdateWasCalled = true;
            LastUpdateRequest = request;
            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;
            return Task.FromResult(NextFeedbackResult with
            {
                ChatInteractionId = request.ChatInteractionId,
                Rating = request.Rating
            });
        }

        public Task<AnswerFeedbackReviewResult> GetReviewDataAsync(CancellationToken ct = default)
        {
            ReviewWasCalled = true;
            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;
            return Task.FromResult(NextReviewResult);
        }

        private static AnswerFeedbackResult FeedbackResult(Guid id, AnswerFeedbackRating rating) =>
            new(
                id,
                Guid.Parse("22222222-2222-4222-8222-222222222222"),
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                OrgId,
                rating,
                DateTimeOffset.Parse("2026-05-30T00:00:00Z"),
                DateTimeOffset.Parse("2026-05-30T00:00:00Z"));
    }

    public sealed class FakeRagChatOrchestrationService : IRagChatOrchestrationService
    {
        public bool WasCalled { get; private set; }
        public AskQuestionRequest? LastRequest { get; private set; }
        public AskQuestionResponse NextResponse { get; set; } = GroundedResponse();
        public Exception? ExceptionToThrow { get; set; }

        public void Reset()
        {
            WasCalled = false;
            LastRequest = null;
            NextResponse = GroundedResponse();
            ExceptionToThrow = null;
        }

        public Task<AskQuestionResponse> AskAsync(
            AskQuestionRequest request,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastRequest = request;
            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;
            return Task.FromResult(NextResponse);
        }
    }

    private sealed class FakeAuthRepository : IUserAuthRepository
    {
        private static readonly Dictionary<string, UserAuthRecord> Users =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [AgentEmail] = User(0xA1, AgentEmail, "Agent", ["Agent"]),
                [SupervisorEmail] = User(0xA2, SupervisorEmail, "Supervisor", ["Supervisor"]),
                [KnowledgeAdminEmail] = User(0xA3, KnowledgeAdminEmail, "Knowledge Admin", ["KnowledgeAdmin"]),
                [ManagerEmail] = User(0xA4, ManagerEmail, "Manager", ["Manager"]),
                [AdminEmail] = User(0xA5, AdminEmail, "Admin", ["Admin"]),
                [NoRoleEmail] = User(0xB1, NoRoleEmail, "No Role", [])
            };

        public Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(Users.GetValueOrDefault(email));

        public Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Users.Values.SingleOrDefault(u => u.UserId == userId));

        public Task UpdateLastLoginAtAsync(Guid userId, DateTimeOffset loginAt, CancellationToken ct = default) =>
            Task.CompletedTask;

        private static UserAuthRecord User(byte suffix, string email, string displayName, IReadOnlyList<string> roles) =>
            new(
                Guid.Parse($"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaa{suffix:x2}"),
                OrgId,
                email,
                displayName,
                Password,
                UserStatus.Active,
                roles);
    }

    private sealed class FixedAccessStateReader(IUserAuthRepository repository) : IUserAccessStateReader
    {
        public async Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await repository.FindByIdAsync(userId, ct);
            if (user is null || user.Status != UserStatus.Active)
                return null;
            return new UserAccessState(user.UserId, user.OrganizationId, user.Roles);
        }
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => password;
        public bool VerifyPassword(string hashedPassword, string password) => hashedPassword == password;
    }

    private sealed class NoopAuditEventWriter : IAuditEventWriter
    {
        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}

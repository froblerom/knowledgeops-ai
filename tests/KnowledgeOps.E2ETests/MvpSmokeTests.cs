using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeOps.Application.Admin;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Application.Chat.Feedback;
using KnowledgeOps.Application.Dashboard;
using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Application.Retrieval;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeOps.E2ETests;

public sealed class MvpSmokeTests : IClassFixture<MvpSmokeTestFactory>
{
    private readonly MvpSmokeTestFactory _factory;

    public MvpSmokeTests(MvpSmokeTestFactory factory)
    {
        _factory = factory;
        _factory.Reset();
    }

    [Fact]
    public async Task AuthAndRoles_AllowLoginAndDenyUnauthorizedDashboardAccess()
    {
        var agent = await AuthenticateAsync(MvpSmokeTestFactory.AgentEmail);
        var manager = await AuthenticateAsync(MvpSmokeTestFactory.ManagerEmail);

        Assert.Equal(HttpStatusCode.Forbidden, (await agent.GetAsync("/api/v1/dashboard/overview")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync("/api/v1/dashboard/overview")).StatusCode);
    }

    [Fact]
    public async Task DocumentUploadAndStatus_ReturnUploadedMetadataWithoutStorageLeak()
    {
        var client = await AuthenticateAsync(MvpSmokeTestFactory.KnowledgeAdminEmail);

        var response = await client.PostAsync("/api/v1/documents", BuildUploadContent());
        var raw = await response.Content.ReadAsStringAsync();
        var body = JsonDocument.Parse(raw).RootElement;
        var documentId = body.GetProperty("documentId").GetGuid();
        var status = await client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/documents/{documentId}/processing-status");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Uploaded", body.GetProperty("processingStatus").GetString());
        Assert.Equal("Uploaded", status.GetProperty("processingStatus").GetString());
        Assert.DoesNotContain("storageLocation", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("local://", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChatQuestion_ReturnsDeterministicGroundedAnswerWithCitations()
    {
        var client = await AuthenticateAsync(MvpSmokeTestFactory.AgentEmail);

        var response = await client.PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { questionText = "What is the escalation policy?" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_factory.RagService.WasCalled);
        Assert.Equal("GroundedAnswer", body.GetProperty("answerState").GetString());
        Assert.False(body.GetProperty("insufficientContext").GetBoolean());
        Assert.Equal("Use the approved escalation process.", body.GetProperty("answer").GetString());
        Assert.Equal(1, body.GetProperty("citations").GetArrayLength());
        Assert.Equal("Escalation Policy", body.GetProperty("citations")[0].GetProperty("documentTitle").GetString());
    }

    [Fact]
    public async Task ChatQuestion_WithNoEligibleContext_ReturnsSafeInsufficientContext()
    {
        var client = await AuthenticateAsync(MvpSmokeTestFactory.AgentEmail);

        var response = await client.PostAsJsonAsync(
            "/api/v1/chat/questions",
            new { questionText = "unknown unpublished exception" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("InsufficientContext", body.GetProperty("answerState").GetString());
        Assert.True(body.GetProperty("insufficientContext").GetBoolean());
        Assert.Empty(body.GetProperty("citations").EnumerateArray());
        Assert.DoesNotContain("chunk text", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Feedback_CanSubmitAndUpdateOwnRating()
    {
        var client = await AuthenticateAsync(MvpSmokeTestFactory.AgentEmail);
        var interactionId = MvpSmokeTestFactory.InteractionId;

        var submitted = await client.PostAsJsonAsync(
            $"/api/v1/chat/interactions/{interactionId}/feedback",
            new { rating = "Useful" });
        var updated = await client.PutAsJsonAsync(
            $"/api/v1/chat/interactions/{interactionId}/feedback",
            new { rating = "NotUseful" });
        var body = await updated.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, submitted.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("NotUseful", body.GetProperty("rating").GetString());
    }

    [Fact]
    public async Task DashboardAdminAndHealth_ReturnSafeOperationalSmokeData()
    {
        var manager = await AuthenticateAsync(MvpSmokeTestFactory.ManagerEmail);
        var admin = await AuthenticateAsync(MvpSmokeTestFactory.AdminEmail);

        var overview = await manager.GetFromJsonAsync<JsonElement>("/api/v1/dashboard/overview");
        var health = await admin.GetFromJsonAsync<JsonElement>("/api/v1/health/details");
        var failures = await admin.GetAsync("/api/v1/admin/processing-failures");
        var audit = await admin.GetAsync("/api/v1/admin/audit-log");
        var rawAudit = await audit.Content.ReadAsStringAsync();

        Assert.Equal(2, overview.GetProperty("questionsAsked").GetInt32());
        Assert.Equal("Healthy", health.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, failures.StatusCode);
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        Assert.DoesNotContain("password", rawAudit, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prompt", rawAudit, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CrossScopeDirectResourceAccess_ReturnsSafeNotFound()
    {
        var client = await AuthenticateAsync(MvpSmokeTestFactory.ManagerEmail);

        var documentResponse = await client.GetAsync($"/api/v1/documents/{MvpSmokeTestFactory.OtherOrgDocumentId}");
        var interactionResponse = await client.GetAsync($"/api/v1/chat/interactions/{MvpSmokeTestFactory.OtherOrgInteractionId}");
        var citationResponse = await client.GetAsync($"/api/v1/chat/interactions/{MvpSmokeTestFactory.OtherOrgInteractionId}/citations");
        var raw = await documentResponse.Content.ReadAsStringAsync()
            + await interactionResponse.Content.ReadAsStringAsync()
            + await citationResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, documentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, interactionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, citationResponse.StatusCode);
        Assert.DoesNotContain(MvpSmokeTestFactory.OtherOrgId.ToString(), raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answerText", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storageLocation", raw, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<HttpClient> AuthenticateAsync(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = MvpSmokeTestFactory.Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.GetProperty("accessToken").GetString());
        return client;
    }

    private static MultipartFormDataContent BuildUploadContent()
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("Escalation Policy"), "title");
        var fileContent = new ByteArrayContent(new byte[1024]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "policy.pdf");
        return content;
    }
}

public sealed class MvpSmokeTestFactory : WebApplicationFactory<Program>
{
    public const string Password = "test-password";
    public const string AgentEmail = "agent.e2e@example.test";
    public const string SupervisorEmail = "supervisor.e2e@example.test";
    public const string KnowledgeAdminEmail = "kadmin.e2e@example.test";
    public const string ManagerEmail = "manager.e2e@example.test";
    public const string AdminEmail = "admin.e2e@example.test";

    public static readonly Guid OrgId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid OtherOrgId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public static readonly Guid InteractionId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    public static readonly Guid OtherOrgInteractionId = Guid.Parse("44444444-4444-4444-8444-444444444444");
    public static readonly Guid OtherOrgDocumentId = Guid.Parse("55555555-5555-4555-8555-555555555555");
    public static readonly Guid DocumentId = Guid.Parse("66666666-6666-4666-8666-666666666666");
    public static readonly Guid ChunkId = Guid.Parse("77777777-7777-4777-8777-777777777777");

    public FakeRagService RagService { get; } = new();
    public FakeFeedbackService FeedbackService { get; } = new();
    public FakeDocumentRepository DocumentRepository { get; } = new();
    public FakeChatHistoryService ChatHistoryService { get; } = new();
    public FakeDashboardService DashboardService { get; } = new();
    public FakeAdminSupportService AdminSupportService { get; } = new();
    public RecordingAuditWriter AuditWriter { get; } = new();

    public void Reset()
    {
        RagService.Reset();
        FeedbackService.Reset();
        DocumentRepository.Reset();
        ChatHistoryService.Reset();
        DashboardService.Reset();
        AdminSupportService.Reset();
        AuditWriter.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "KnowledgeOps",
                ["Jwt:Audience"] = "KnowledgeOps",
                ["Jwt:SigningKey"] = "issue-46-e2e-signing-key-that-is-long-enough",
                ["Jwt:ExpirationMinutes"] = "60",
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=Issue46Smoke;Trusted_Connection=True;"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserAuthRepository>();
            services.AddScoped<IUserAuthRepository, FakeAuthRepository>();
            services.RemoveAll<IUserAccessStateReader>();
            services.AddScoped<IUserAccessStateReader>(sp =>
                new FixedAccessStateReader(sp.GetRequiredService<IUserAuthRepository>()));
            services.RemoveAll<IPasswordHasher>();
            services.AddSingleton<IPasswordHasher, TestPasswordHasher>();
            services.RemoveAll<IRagChatOrchestrationService>();
            services.AddSingleton<IRagChatOrchestrationService>(RagService);
            services.RemoveAll<IAnswerFeedbackService>();
            services.AddSingleton<IAnswerFeedbackService>(FeedbackService);
            services.RemoveAll<IDocumentRepository>();
            services.AddSingleton<IDocumentRepository>(DocumentRepository);
            services.RemoveAll<IDocumentStorage>();
            services.AddSingleton<IDocumentStorage, FakeDocumentStorage>();
            services.RemoveAll<IChatHistoryService>();
            services.AddSingleton<IChatHistoryService>(ChatHistoryService);
            services.RemoveAll<IDashboardService>();
            services.AddSingleton<IDashboardService>(DashboardService);
            services.RemoveAll<IAdminSupportService>();
            services.AddSingleton<IAdminSupportService>(AdminSupportService);
            services.RemoveAll<IAuditEventWriter>();
            services.AddSingleton<IAuditEventWriter>(AuditWriter);
            services.RemoveAll<IDatabaseHealthCheck>();
            services.AddSingleton<IDatabaseHealthCheck, HealthyDatabaseCheck>();
            services.RemoveAll<IRetrievalStorageHealthCheck>();
            services.AddSingleton<IRetrievalStorageHealthCheck, HealthyRetrievalCheck>();
        });
    }

    public sealed class FakeRagService : IRagChatOrchestrationService
    {
        public bool WasCalled { get; private set; }
        public bool RetrievalBeforeGenerationVerified { get; private set; }

        public void Reset()
        {
            WasCalled = false;
            RetrievalBeforeGenerationVerified = false;
        }

        public Task<AskQuestionResponse> AskAsync(AskQuestionRequest request, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            RetrievalBeforeGenerationVerified = true;
            if (request.QuestionText.Contains("unknown", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new AskQuestionResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    AnswerState.InsufficientContext,
                    "The approved documents do not contain enough information to answer this question safely.",
                    0,
                    true,
                    "e2e-insufficient",
                    []));
            }

            return Task.FromResult(new AskQuestionResponse(
                InteractionId,
                Guid.Parse("88888888-8888-4888-8888-888888888888"),
                AnswerState.Grounded,
                "Use the approved escalation process.",
                1,
                false,
                "e2e-grounded",
                [
                    new CitationResponse(
                        Guid.NewGuid(),
                        DocumentId,
                        ChunkId,
                        1,
                        "Escalation Policy",
                        2,
                        "Approved Steps",
                        0.92)
                ]));
        }
    }

    public sealed class FakeFeedbackService : IAnswerFeedbackService
    {
        private AnswerFeedbackRating? _rating;

        public void Reset() => _rating = null;

        public Task<AnswerFeedbackResult> SubmitAsync(SubmitAnswerFeedbackRequest request, CancellationToken ct = default)
        {
            if (_rating is not null)
                throw new ApplicationConflictException();
            _rating = request.Rating;
            return Task.FromResult(Result(request.ChatInteractionId, request.Rating));
        }

        public Task<AnswerFeedbackResult> UpdateOwnAsync(UpdateAnswerFeedbackRequest request, CancellationToken ct = default)
        {
            if (_rating is null)
                throw new ApplicationNotFoundException();
            _rating = request.Rating;
            return Task.FromResult(Result(request.ChatInteractionId, request.Rating));
        }

        public Task<AnswerFeedbackReviewResult> GetReviewDataAsync(CancellationToken ct = default) =>
            Task.FromResult(new AnswerFeedbackReviewResult(1, 1, []));

        private static AnswerFeedbackResult Result(Guid interactionId, AnswerFeedbackRating rating) =>
            new(Guid.NewGuid(), interactionId, Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"),
                OrgId, rating, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    }

    public sealed class FakeDocumentRepository : IDocumentRepository
    {
        private readonly List<ManagedDocument> _documents = [];

        public void Reset()
        {
            _documents.Clear();
            _documents.Add(Document(OtherOrgDocumentId, OtherOrgId, "Other org", DocumentProcessingStatus.Processed));
        }

        public Task<IReadOnlyList<ManagedDocument>> ListAsync(Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ManagedDocument>>(_documents.Where(d => d.OrganizationId == organizationId).ToArray());

        public Task<ManagedDocument?> FindAsync(Guid documentId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(_documents.SingleOrDefault(d => d.DocumentId == documentId && d.OrganizationId == organizationId));

        public Task<IReadOnlyList<ManagedDocument>> FindFailedDocumentsAsync(Guid organizationId, int limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ManagedDocument>>(
                _documents.Where(d => d.OrganizationId == organizationId && d.ProcessingStatus == DocumentProcessingStatus.Failed)
                    .Take(limit).ToArray());

        public Task<ManagedDocument> CreateAsync(NewManagedDocument document, CancellationToken ct = default)
        {
            var created = new ManagedDocument(
                document.DocumentId,
                document.OrganizationId,
                document.FileName,
                document.Title,
                document.ContentType,
                document.FileSizeBytes,
                document.StorageLocation,
                DocumentProcessingStatus.Uploaded,
                null,
                false,
                document.UploadedByUserId,
                document.UploadedAt,
                null,
                null,
                document.CreatedAt,
                document.CreatedAt,
                null);
            _documents.Add(created);
            return Task.FromResult(created);
        }

        public Task<DocumentDisableResult?> DisableRetrievalAsync(Guid documentId, Guid organizationId, DateTimeOffset updatedAt, CancellationToken ct = default) =>
            Task.FromResult<DocumentDisableResult?>(null);

        public Task<DocumentEnableResult?> EnableRetrievalAsync(Guid documentId, Guid organizationId, DateTimeOffset updatedAt, CancellationToken ct = default) =>
            Task.FromResult<DocumentEnableResult?>(null);

        public Task<IReadOnlyList<ManagedDocument>> FindPendingForProcessingAsync(int maxCount, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ManagedDocument>>([]);

        public Task<ManagedDocument?> ClaimForProcessingAsync(Guid documentId, DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);

        public Task<ManagedDocument?> MarkProcessedAsync(Guid documentId, DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);

        public Task<ManagedDocument?> MarkFailedAsync(Guid documentId, string safeFailureReason, DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);

        private static ManagedDocument Document(Guid id, Guid orgId, string title, DocumentProcessingStatus status) =>
            new(id, orgId, "policy.pdf", title, "application/pdf", 100,
                "local://must-not-leak", status, status == DocumentProcessingStatus.Failed ? "Safe failure." : null,
                status == DocumentProcessingStatus.Processed, Guid.NewGuid(), DateTimeOffset.UtcNow,
                null, status == DocumentProcessingStatus.Processed ? DateTimeOffset.UtcNow : null,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);
    }

    public sealed class FakeChatHistoryService : IChatHistoryService
    {
        public void Reset() { }
        public Task<IReadOnlyList<ChatSessionSummaryDto>> GetSessionsAsync(bool scopedReview, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ChatSessionSummaryDto>>([]);
        public Task<Guid> CreateSessionAsync(string? title, CancellationToken ct = default) =>
            Task.FromResult(Guid.NewGuid());
        public Task<ChatSessionDetailDto?> GetSessionAsync(Guid sessionId, CancellationToken ct = default) =>
            Task.FromResult<ChatSessionDetailDto?>(null);
        public Task<ChatInteractionDetailDto?> GetInteractionAsync(Guid interactionId, CancellationToken ct = default) =>
            Task.FromResult(interactionId == OtherOrgInteractionId ? null : SampleInteraction(interactionId));
        public Task<IReadOnlyList<ChatCitationHistoryDto>?> GetInteractionCitationsAsync(Guid interactionId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ChatCitationHistoryDto>?>(interactionId == OtherOrgInteractionId ? null : []);

        private static ChatInteractionDetailDto SampleInteraction(Guid interactionId) =>
            new(interactionId, Guid.NewGuid(), "GroundedAnswer", false, "Question", "Answer",
                "rag-grounded-v1", "e2e-corr",
                new ChatRetrievalMetadataDto(1, 10, 20, 30, null, null, null, null, null, null),
                [], DateTimeOffset.UtcNow);
    }

    public sealed class FakeDashboardService : IDashboardService
    {
        public void Reset() { }
        public Task<DashboardOverviewResult> GetOverviewAsync(DashboardDateRange range, CancellationToken ct = default) =>
            Task.FromResult(new DashboardOverviewResult(2, 1, 1, 1, 0, 30, 1, 0, 1, 1, true, 0.01m));
        public Task<DashboardDocumentsResult> GetDocumentsAsync(DashboardDateRange range, CancellationToken ct = default) =>
            Task.FromResult(new DashboardDocumentsResult(0, 0, 1, 0, 0));
        public Task<DashboardChatResult> GetChatAsync(DashboardDateRange range, CancellationToken ct = default) =>
            Task.FromResult(new DashboardChatResult(2, 1, 30, 10, 20, 30, 1, 0, 100, 50, true, 0.01m));
        public Task<DashboardFeedbackResult> GetFeedbackAsync(DashboardDateRange range, CancellationToken ct = default) =>
            Task.FromResult(new DashboardFeedbackResult(1, 1));
    }

    public sealed class FakeAdminSupportService : IAdminSupportService
    {
        public void Reset() { }
        public Task<IReadOnlyList<ProcessingFailureResult>> GetProcessingFailuresAsync(int? limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProcessingFailureResult>>([
                new(Guid.NewGuid(), "Failed policy", "Failed", "Unsupported document encoding.", DateTimeOffset.UtcNow)
            ]);
        public Task<IReadOnlyList<AuditLogResult>> GetAuditLogAsync(AuditLogQuery query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AuditLogResult>>([
                new(Guid.NewGuid(), "DocumentUploadAccepted", "Safe audit row.", "Info",
                    Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"), "Document", Guid.NewGuid(), "e2e-corr", DateTimeOffset.UtcNow)
            ]);
    }

    public sealed class RecordingAuditWriter : IAuditEventWriter
    {
        public List<AuditEvent> Events { get; } = [];
        public void Reset() => Events.Clear();
        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    public sealed class FakeDocumentStorage : IDocumentStorage
    {
        public Task<StoredDocumentReference> StoreAsync(Stream fileStream, string safeFileName, string contentType, CancellationToken cancellationToken = default) =>
            Task.FromResult(new StoredDocumentReference($"local://test/{safeFileName}"));
        public Task<Stream> OpenReadAsync(string storageReference, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(Stream.Null);
        public Task DeleteAsync(string storageReference, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    public sealed class HealthyDatabaseCheck : IDatabaseHealthCheck
    {
        public Task<DatabaseHealthResult> CheckAsync(CancellationToken ct = default) =>
            Task.FromResult(DatabaseHealthResult.Healthy);
    }

    public sealed class HealthyRetrievalCheck : IRetrievalStorageHealthCheck
    {
        public Task<RetrievalStorageHealthResult> CheckAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new RetrievalStorageHealthResult(true, "LocalSqlVectorStore", 1, 0));
    }

    private sealed class FakeAuthRepository : IUserAuthRepository
    {
        private static readonly Dictionary<string, UserAuthRecord> Users =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [AgentEmail] = User("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1", AgentEmail, "Agent", ["Agent"]),
                [SupervisorEmail] = User("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2", SupervisorEmail, "Supervisor", ["Supervisor"]),
                [KnowledgeAdminEmail] = User("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3", KnowledgeAdminEmail, "Knowledge Admin", ["KnowledgeAdmin"]),
                [ManagerEmail] = User("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa4", ManagerEmail, "Manager", ["Manager"]),
                [AdminEmail] = User("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa5", AdminEmail, "Admin", ["Admin"])
            };

        public Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(Users.GetValueOrDefault(email));
        public Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Users.Values.SingleOrDefault(u => u.UserId == userId));
        public Task UpdateLastLoginAtAsync(Guid userId, DateTimeOffset loginAt, CancellationToken ct = default) =>
            Task.CompletedTask;

        private static UserAuthRecord User(string id, string email, string displayName, IReadOnlyList<string> roles) =>
            new(Guid.Parse(id), OrgId, email, displayName, Password, UserStatus.Active, roles);
    }

    private sealed class FixedAccessStateReader(IUserAuthRepository repository) : IUserAccessStateReader
    {
        public async Task<UserAccessState?> FindActiveByIdAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await repository.FindByIdAsync(userId, ct);
            return user is null || user.Status != UserStatus.Active
                ? null
                : new UserAccessState(user.UserId, user.OrganizationId, user.Roles);
        }
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => password;
        public bool VerifyPassword(string hashedPassword, string password) => hashedPassword == password;
    }
}

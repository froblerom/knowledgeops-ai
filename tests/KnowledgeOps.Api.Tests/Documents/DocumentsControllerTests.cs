using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Documents;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Application.Users;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeOps.Api.Tests.Documents;

public sealed class DocumentsControllerTests : IClassFixture<DocumentsApiTestFactory>
{
    private readonly DocumentsApiTestFactory _factory;

    public DocumentsControllerTests(DocumentsApiTestFactory factory)
    {
        _factory = factory;
        _factory.Reset();
    }

    [Fact]
    public async Task List_WithoutAuthenticationReturns401()
    {
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _factory.CreateClient().GetAsync("/api/v1/documents")).StatusCode);
    }

    [Theory]
    [InlineData(DocumentsApiTestFactory.AgentEmail)]
    [InlineData(DocumentsApiTestFactory.SupervisorEmail)]
    public async Task List_WithoutViewPermissionReturns403(string email)
    {
        var client = await AuthenticateAsync(email);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/documents")).StatusCode);
    }

    [Theory]
    [InlineData(DocumentsApiTestFactory.KnowledgeAdminEmail)]
    [InlineData(DocumentsApiTestFactory.ManagerEmail)]
    [InlineData(DocumentsApiTestFactory.AdminEmail)]
    public async Task List_AllowedRolesReturnOnlyActorOrganizationDocuments(string email)
    {
        _factory.DocumentRepository.AddDocument(
            MakeDocument(DocumentsApiTestFactory.DocId, DocumentsApiTestFactory.OrgId));
        _factory.DocumentRepository.AddDocument(
            MakeDocument(Guid.NewGuid(), DocumentsApiTestFactory.OtherOrgId));

        var response = await (await AuthenticateAsync(email)).GetAsync("/api/v1/documents");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal(DocumentsApiTestFactory.DocId, body[0].GetProperty("documentId").GetGuid());
    }

    [Theory]
    [InlineData(DocumentsApiTestFactory.KnowledgeAdminEmail)]
    [InlineData(DocumentsApiTestFactory.ManagerEmail)]
    [InlineData(DocumentsApiTestFactory.AdminEmail)]
    public async Task Get_AllowedRolesReturnCanonicalMetadata(string email)
    {
        _factory.DocumentRepository.AddDocument(
            MakeDocument(DocumentsApiTestFactory.DocId, DocumentsApiTestFactory.OrgId));

        var response = await (await AuthenticateAsync(email)).GetAsync(
            $"/api/v1/documents/{DocumentsApiTestFactory.DocId}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("policy.pdf", body.GetProperty("fileName").GetString());
        Assert.Equal("application/pdf", body.GetProperty("contentType").GetString());
        Assert.Equal(42, body.GetProperty("fileSizeBytes").GetInt64());
        Assert.True(body.TryGetProperty("uploadedAt", out _));
    }

    [Theory]
    [InlineData(DocumentsApiTestFactory.AgentEmail)]
    [InlineData(DocumentsApiTestFactory.SupervisorEmail)]
    public async Task Get_WithoutViewPermissionReturns403(string email)
    {
        var response = await (await AuthenticateAsync(email)).GetAsync(
            $"/api/v1/documents/{DocumentsApiTestFactory.DocId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_CrossOrganizationReturnsSafe404()
    {
        _factory.DocumentRepository.AddDocument(
            MakeDocument(DocumentsApiTestFactory.DocId, DocumentsApiTestFactory.OtherOrgId));

        var response = await (await AuthenticateAsync(DocumentsApiTestFactory.ManagerEmail)).GetAsync(
            $"/api/v1/documents/{DocumentsApiTestFactory.DocId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(DocumentsApiTestFactory.KnowledgeAdminEmail)]
    [InlineData(DocumentsApiTestFactory.ManagerEmail)]
    [InlineData(DocumentsApiTestFactory.AdminEmail)]
    public async Task GetProcessingStatus_AllowedRolesReturnStatusContract(string email)
    {
        _factory.DocumentRepository.AddDocument(
            MakeDocument(DocumentsApiTestFactory.DocId, DocumentsApiTestFactory.OrgId));

        var response = await (await AuthenticateAsync(email)).GetAsync(
            $"/api/v1/documents/{DocumentsApiTestFactory.DocId}/processing-status");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Uploaded", body.GetProperty("processingStatus").GetString());
        Assert.True(body.TryGetProperty("failureReason", out _));
        Assert.True(body.TryGetProperty("processingStartedAt", out _));
    }

    [Theory]
    [InlineData(DocumentsApiTestFactory.AgentEmail)]
    [InlineData(DocumentsApiTestFactory.SupervisorEmail)]
    public async Task GetProcessingStatus_WithoutPermissionReturns403(string email)
    {
        var response = await (await AuthenticateAsync(email)).GetAsync(
            $"/api/v1/documents/{DocumentsApiTestFactory.DocId}/processing-status");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProcessingStatus_CrossOrganizationReturnsSafe404()
    {
        _factory.DocumentRepository.AddDocument(
            MakeDocument(DocumentsApiTestFactory.DocId, DocumentsApiTestFactory.OtherOrgId));

        var response = await (await AuthenticateAsync(DocumentsApiTestFactory.ManagerEmail)).GetAsync(
            $"/api/v1/documents/{DocumentsApiTestFactory.DocId}/processing-status");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Disable_WithoutAuthenticationReturns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/documents/{DocumentsApiTestFactory.DocId}/disable",
            new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(DocumentsApiTestFactory.AgentEmail)]
    [InlineData(DocumentsApiTestFactory.SupervisorEmail)]
    [InlineData(DocumentsApiTestFactory.ManagerEmail)]
    public async Task Disable_WithoutDisablePermissionReturns403(string email)
    {
        var response = await (await AuthenticateAsync(email)).PostAsJsonAsync(
            $"/api/v1/documents/{DocumentsApiTestFactory.DocId}/disable",
            new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(DocumentsApiTestFactory.KnowledgeAdminEmail)]
    [InlineData(DocumentsApiTestFactory.AdminEmail)]
    public async Task Disable_AllowedRolesDisableWithoutChangingStatusAndAuditTransitionOnce(string email)
    {
        _factory.DocumentRepository.AddDocument(
            MakeDocument(
                DocumentsApiTestFactory.DocId,
                DocumentsApiTestFactory.OrgId,
                DocumentProcessingStatus.Processed,
                isRetrievalEnabled: true));

        var client = await AuthenticateAsync(email);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/documents/{DocumentsApiTestFactory.DocId}/disable",
            new { });
        var repeated = await client.PostAsJsonAsync(
            $"/api/v1/documents/{DocumentsApiTestFactory.DocId}/disable",
            new { });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.False(body.GetProperty("isRetrievalEnabled").GetBoolean());
        Assert.Equal("Processed", body.GetProperty("processingStatus").GetString());
        Assert.Single(_factory.Audit.Events, e => e.EventType == AuditEventTypes.DocumentRetrievalDisabled);
    }

    [Fact]
    public async Task Disable_CrossOrganizationReturnsSafe404()
    {
        _factory.DocumentRepository.AddDocument(
            MakeDocument(DocumentsApiTestFactory.DocId, DocumentsApiTestFactory.OtherOrgId, isRetrievalEnabled: true));

        var response = await (await AuthenticateAsync(DocumentsApiTestFactory.KnowledgeAdminEmail)).PostAsJsonAsync(
            $"/api/v1/documents/{DocumentsApiTestFactory.DocId}/disable",
            new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ResponseDoesNotExposeStorageLocationOrInternalFields()
    {
        _factory.DocumentRepository.AddDocument(
            MakeDocument(DocumentsApiTestFactory.DocId, DocumentsApiTestFactory.OrgId));

        var raw = await (await (await AuthenticateAsync(DocumentsApiTestFactory.ManagerEmail)).GetAsync(
            $"/api/v1/documents/{DocumentsApiTestFactory.DocId}")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("storageLocation", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pending://", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("organizationId", raw, StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────────────────────────
    // Upload — authorization
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_WithoutAuthentication_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync(
            "/api/v1/documents",
            BuildUploadContent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(DocumentsApiTestFactory.AgentEmail)]
    [InlineData(DocumentsApiTestFactory.SupervisorEmail)]
    [InlineData(DocumentsApiTestFactory.ManagerEmail)]
    public async Task Upload_WithoutUploadPermission_Returns403(string email)
    {
        var response = await (await AuthenticateAsync(email)).PostAsync(
            "/api/v1/documents",
            BuildUploadContent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(DocumentsApiTestFactory.KnowledgeAdminEmail)]
    [InlineData(DocumentsApiTestFactory.AdminEmail)]
    public async Task Upload_AllowedRoles_Returns201WithUploadedStatus(string email)
    {
        var response = await (await AuthenticateAsync(email)).PostAsync(
            "/api/v1/documents",
            BuildUploadContent());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Uploaded", body.GetProperty("processingStatus").GetString());
        Assert.False(body.GetProperty("isRetrievalEnabled").GetBoolean());
    }

    // ──────────────────────────────────────────────────────────────
    // Upload — validation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_MissingTitle_Returns400()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[1024]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "policy.pdf");

        var response = await (await AuthenticateAsync(DocumentsApiTestFactory.KnowledgeAdminEmail))
            .PostAsync("/api/v1/documents", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_MissingFile_Returns400()
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("Policy Document"), "title");

        var response = await (await AuthenticateAsync(DocumentsApiTestFactory.KnowledgeAdminEmail))
            .PostAsync("/api/v1/documents", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_UnsupportedExtension_Returns400()
    {
        var response = await (await AuthenticateAsync(DocumentsApiTestFactory.KnowledgeAdminEmail)).PostAsync(
            "/api/v1/documents",
            BuildUploadContent(fileName: "malware.exe", contentType: "application/octet-stream"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_UnsupportedContentType_Returns400()
    {
        var response = await (await AuthenticateAsync(DocumentsApiTestFactory.KnowledgeAdminEmail)).PostAsync(
            "/api/v1/documents",
            BuildUploadContent(contentType: "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_OversizedFile_Returns400()
    {
        var response = await (await AuthenticateAsync(DocumentsApiTestFactory.KnowledgeAdminEmail)).PostAsync(
            "/api/v1/documents",
            BuildUploadContent(fileSizeBytes: 10 * 1024 * 1024 + 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ──────────────────────────────────────────────────────────────
    // Upload — response contract
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_Success_ResponseDoesNotExposeStorageLocation()
    {
        var raw = await (await (await AuthenticateAsync(DocumentsApiTestFactory.KnowledgeAdminEmail))
            .PostAsync("/api/v1/documents", BuildUploadContent())).Content.ReadAsStringAsync();

        Assert.DoesNotContain("storageLocation", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("local://", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_Success_ResponseDoesNotExposeOrganizationId()
    {
        var raw = await (await (await AuthenticateAsync(DocumentsApiTestFactory.KnowledgeAdminEmail))
            .PostAsync("/api/v1/documents", BuildUploadContent())).Content.ReadAsStringAsync();

        Assert.DoesNotContain("organizationId", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_Success_ResponseShowsExpectedNullFields()
    {
        var response = await (await AuthenticateAsync(DocumentsApiTestFactory.KnowledgeAdminEmail)).PostAsync(
            "/api/v1/documents", BuildUploadContent());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Null, body.GetProperty("failureReason").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("processingStartedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("processedAt").ValueKind);
    }

    [Fact]
    public async Task Upload_Success_LocationHeaderPointsToDocumentDetail()
    {
        var response = await (await AuthenticateAsync(DocumentsApiTestFactory.KnowledgeAdminEmail)).PostAsync(
            "/api/v1/documents", BuildUploadContent());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var documentId = body.GetProperty("documentId").GetGuid();

        Assert.NotNull(response.Headers.Location);
        Assert.Contains(documentId.ToString(), response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Upload_Success_DocumentAssignedToActorOrganization()
    {
        var response = await (await AuthenticateAsync(DocumentsApiTestFactory.KnowledgeAdminEmail)).PostAsync(
            "/api/v1/documents", BuildUploadContent());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var documentId = body.GetProperty("documentId").GetGuid();

        var listed = await (await AuthenticateAsync(DocumentsApiTestFactory.KnowledgeAdminEmail))
            .GetAsync("/api/v1/documents");
        var list = await listed.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal(documentId, list[0].GetProperty("documentId").GetGuid());
    }

    [Fact]
    public async Task Upload_Success_EmitsUploadAcceptedAuditEvent()
    {
        await (await AuthenticateAsync(DocumentsApiTestFactory.KnowledgeAdminEmail)).PostAsync(
            "/api/v1/documents", BuildUploadContent());

        Assert.Single(_factory.Audit.Events, e => e.EventType == AuditEventTypes.DocumentUploadAccepted);
    }

    // ──────────────────────────────────────────────────────────────
    // Upload helpers
    // ──────────────────────────────────────────────────────────────

    private static MultipartFormDataContent BuildUploadContent(
        string title = "Policy Document",
        string fileName = "policy.pdf",
        string contentType = "application/pdf",
        int fileSizeBytes = 1024)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "title");
        var fileContent = new ByteArrayContent(new byte[fileSizeBytes]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    private async Task<HttpClient> AuthenticateAsync(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = DocumentsApiTestFactory.Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.GetProperty("accessToken").GetString());
        return client;
    }

    private static ManagedDocument MakeDocument(
        Guid documentId,
        Guid organizationId,
        DocumentProcessingStatus status = DocumentProcessingStatus.Uploaded,
        bool isRetrievalEnabled = false) =>
        new(
            documentId,
            organizationId,
            "policy.pdf",
            "Test Document",
            "application/pdf",
            42,
            "pending://document-metadata-only",
            status,
            null,
            isRetrievalEnabled,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            null,
            status == DocumentProcessingStatus.Processed ? DateTimeOffset.UtcNow : null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null);
}

public sealed class DocumentsApiTestFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@example.test";
    public const string AgentEmail = "agent@example.test";
    public const string SupervisorEmail = "supervisor@example.test";
    public const string ManagerEmail = "manager@example.test";
    public const string KnowledgeAdminEmail = "ka@example.test";
    public const string Password = "test-password";
    public static readonly Guid OrgId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid OtherOrgId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    public static readonly Guid AgentUserId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    public static readonly Guid SupervisorUserId = Guid.Parse("abababab-abab-4bab-8bab-abababababab");
    public static readonly Guid ManagerUserId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    public static readonly Guid KnowledgeAdminUserId = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");
    public static readonly Guid DocId = Guid.Parse("ee000000-0000-4000-8000-000000000001");

    public FakeDocumentRepository DocumentRepository { get; } = new();
    public FakeDocumentStorage DocumentStorage { get; } = new();
    public RecordingAuditWriter Audit { get; } = new();

    public void Reset()
    {
        DocumentRepository.Reset();
        DocumentStorage.Reset();
        Audit.Events.Clear();
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
            services.RemoveAll<IDocumentRepository>();
            services.AddSingleton<IDocumentRepository>(DocumentRepository);
            services.RemoveAll<IDocumentStorage>();
            services.AddSingleton<IDocumentStorage>(DocumentStorage);
            services.RemoveAll<IPasswordHasher>();
            services.AddSingleton<IPasswordHasher, TestPasswordHasher>();
            services.RemoveAll<IAuditEventWriter>();
            services.AddSingleton<IAuditEventWriter>(Audit);
        });
    }

    public sealed class FakeDocumentRepository : IDocumentRepository
    {
        private readonly List<ManagedDocument> _documents = [];

        public void Reset() => _documents.Clear();
        public void AddDocument(ManagedDocument doc) => _documents.Add(doc);

        public Task<IReadOnlyList<ManagedDocument>> ListAsync(Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ManagedDocument>>(
                _documents.Where(d => d.OrganizationId == organizationId).ToArray());

        public Task<ManagedDocument?> FindAsync(Guid documentId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(_documents.SingleOrDefault(
                d => d.DocumentId == documentId && d.OrganizationId == organizationId));

        public Task<ManagedDocument> CreateAsync(NewManagedDocument document, CancellationToken ct = default)
        {
            var doc = new ManagedDocument(
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
            _documents.Add(doc);
            return Task.FromResult(doc);
        }

        public Task<DocumentDisableResult?> DisableRetrievalAsync(
            Guid documentId,
            Guid organizationId,
            DateTimeOffset updatedAt,
            CancellationToken ct = default)
        {
            var idx = _documents.FindIndex(
                d => d.DocumentId == documentId && d.OrganizationId == organizationId);
            if (idx < 0)
                return Task.FromResult<DocumentDisableResult?>(null);

            var changed = _documents[idx].IsRetrievalEnabled;
            var updated = changed
                ? _documents[idx] with { IsRetrievalEnabled = false, UpdatedAt = updatedAt }
                : _documents[idx];
            _documents[idx] = updated;
            return Task.FromResult<DocumentDisableResult?>(new DocumentDisableResult(updated, changed));
        }

        public Task<IReadOnlyList<ManagedDocument>> FindPendingForProcessingAsync(int maxCount, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ManagedDocument>>([]);

        public Task<ManagedDocument?> ClaimForProcessingAsync(Guid documentId, DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);

        public Task<ManagedDocument?> MarkProcessedAsync(Guid documentId, DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);

        public Task<ManagedDocument?> MarkFailedAsync(Guid documentId, string safeFailureReason, DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult<ManagedDocument?>(null);
    }

    private sealed class FakeAuthRepository : IUserAuthRepository
    {
        private static readonly Dictionary<string, UserAuthRecord> Users =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [AdminEmail] = new(AdminUserId, OrgId, AdminEmail, "Admin", Password, UserStatus.Active, ["Admin"]),
                [AgentEmail] = new(AgentUserId, OrgId, AgentEmail, "Agent", Password, UserStatus.Active, ["Agent"]),
                [SupervisorEmail] = new(SupervisorUserId, OrgId, SupervisorEmail, "Supervisor", Password, UserStatus.Active, ["Supervisor"]),
                [ManagerEmail] = new(ManagerUserId, OrgId, ManagerEmail, "Manager", Password, UserStatus.Active, ["Manager"]),
                [KnowledgeAdminEmail] = new(KnowledgeAdminUserId, OrgId, KnowledgeAdminEmail, "KA", Password, UserStatus.Active, ["KnowledgeAdmin"])
            };

        public Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(Users.GetValueOrDefault(email));
        public Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Users.Values.SingleOrDefault(u => u.UserId == userId));
        public Task UpdateLastLoginAtAsync(Guid userId, DateTimeOffset loginAt, CancellationToken ct = default) =>
            Task.CompletedTask;
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

    public sealed class RecordingAuditWriter : IAuditEventWriter
    {
        public List<AuditEvent> Events { get; } = [];
        public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    public sealed class FakeDocumentStorage : IDocumentStorage
    {
        private readonly List<string> _stored = [];
        public IReadOnlyList<string> Stored => _stored;
        public bool ShouldFailStore { get; set; }

        public void Reset()
        {
            _stored.Clear();
            ShouldFailStore = false;
        }

        public Task<StoredDocumentReference> StoreAsync(
            Stream fileStream, string safeFileName, string contentType, CancellationToken ct = default)
        {
            if (ShouldFailStore)
                throw new InvalidOperationException("Simulated storage failure.");
            var reference = $"local://test-{safeFileName}";
            _stored.Add(reference);
            return Task.FromResult(new StoredDocumentReference(reference));
        }

        public Task<Stream> OpenReadAsync(string storageReference, CancellationToken ct = default) =>
            Task.FromResult<Stream>(Stream.Null);

        public Task DeleteAsync(string storageReference, CancellationToken ct = default)
        {
            _stored.Remove(storageReference);
            return Task.CompletedTask;
        }
    }
}

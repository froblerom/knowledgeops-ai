using KnowledgeOps.Application.Admin;
using KnowledgeOps.Application.Chat;
using KnowledgeOps.Application.Chat.Citations;
using KnowledgeOps.Application.Chat.Feedback;
using KnowledgeOps.Application.Dashboard;
using KnowledgeOps.Application.Documents;
using KnowledgeOps.Domain.Audit;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using KnowledgeOps.Infrastructure;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeOps.IntegrationTests;

public sealed class MvpRepositorySqlTests
{
    [SqlServerFact]
    public async Task FeedbackRepository_PersistsUpdatesAndScopesByOrganization()
    {
        await RunInDatabaseAsync(async (provider, context) =>
        {
            var feedbackRepository = provider.GetRequiredService<IAnswerFeedbackRepository>();
            var now = DateTimeOffset.UtcNow;
            var orgA = CreateOrganization("Feedback Org A", now);
            var orgB = CreateOrganization("Feedback Org B", now);
            var userA = CreateUser(orgA.Id, $"feedback-a-{Guid.NewGuid():N}@example.test", now);
            var userB = CreateUser(orgB.Id, $"feedback-b-{Guid.NewGuid():N}@example.test", now);
            var interactionA = await SeedInteractionAsync(context, orgA, userA, now);
            await SeedInteractionAsync(context, orgB, userB, now);

            var feedback = AnswerFeedback.Create(
                orgA.Id,
                userA.Id,
                interactionA.Id,
                AnswerFeedbackRating.Useful);
            await feedbackRepository.AddAsync(feedback);
            await feedbackRepository.SaveChangesAsync();

            var stored = await feedbackRepository.FindByInteractionAndUserAsync(interactionA.Id, userA.Id, orgA.Id);
            var crossOrg = await feedbackRepository.FindByInteractionAndUserAsync(interactionA.Id, userA.Id, orgB.Id);
            Assert.NotNull(stored);
            Assert.Null(crossOrg);

            stored.UpdateRating(AnswerFeedbackRating.NotUseful);
            await feedbackRepository.SaveChangesAsync();

            var updated = await context.AnswerFeedback.AsNoTracking().SingleAsync(f => f.Id == feedback.Id);
            Assert.Equal(AnswerFeedbackRating.NotUseful, updated.Rating);
            Assert.Single(await feedbackRepository.ListForReviewAsync(orgA.Id));
            Assert.Empty(await feedbackRepository.ListForReviewAsync(orgB.Id));
        });
    }

    [SqlServerFact]
    public async Task FeedbackRepository_UniqueIndexPreventsDuplicateFeedbackForSameInteractionAndUser()
    {
        await RunInDatabaseAsync(async (provider, context) =>
        {
            var feedbackRepository = provider.GetRequiredService<IAnswerFeedbackRepository>();
            var now = DateTimeOffset.UtcNow;
            var org = CreateOrganization("Unique Feedback Org", now);
            var user = CreateUser(org.Id, $"unique-feedback-{Guid.NewGuid():N}@example.test", now);
            var interaction = await SeedInteractionAsync(context, org, user, now);

            await feedbackRepository.AddAsync(AnswerFeedback.Create(
                org.Id,
                user.Id,
                interaction.Id,
                AnswerFeedbackRating.Useful));
            await feedbackRepository.SaveChangesAsync();

            await feedbackRepository.AddAsync(AnswerFeedback.Create(
                org.Id,
                user.Id,
                interaction.Id,
                AnswerFeedbackRating.NotUseful));

            await Assert.ThrowsAnyAsync<DbUpdateException>(() => feedbackRepository.SaveChangesAsync());
        });
    }

    [SqlServerFact]
    public async Task ChatInteractionAndCitationRepositories_PersistHistoryAndFilterByOrganization()
    {
        await RunInDatabaseAsync(async (provider, context) =>
        {
            var interactionRepository = provider.GetRequiredService<IChatInteractionRepository>();
            var citationRepository = provider.GetRequiredService<ICitationRepository>();
            var now = DateTimeOffset.UtcNow;
            var orgA = CreateOrganization("Chat Org A", now);
            var orgB = CreateOrganization("Chat Org B", now);
            var userA = CreateUser(orgA.Id, $"chat-a-{Guid.NewGuid():N}@example.test", now);
            var userB = CreateUser(orgB.Id, $"chat-b-{Guid.NewGuid():N}@example.test", now);
            context.AddRange(orgA, orgB, userA, userB);
            await context.SaveChangesAsync();

            var sessionA = ChatSession.Create(orgA.Id, userA.Id, "Org A session");
            var sessionB = ChatSession.Create(orgB.Id, userB.Id, "Org B session");
            context.ChatSessions.AddRange(sessionA, sessionB);
            await context.SaveChangesAsync();

            var interactionA = ChatInteraction.Create(sessionA.Id, orgA.Id, userA.Id, "A question", "hash-a", "corr-a");
            interactionA.RecordGroundedOutcome(
                "Grounded answer.",
                Guid.NewGuid(),
                candidateCount: 2,
                retrievalMs: 10,
                generationMs: 20,
                totalMs: 30,
                inputTokens: 40,
                outputTokens: 20,
                cost: 0.001m,
                provider: "Fake",
                model: "fake-deterministic-v1");
            var interactionB = ChatInteraction.Create(sessionB.Id, orgB.Id, userB.Id, "B question", "hash-b", "corr-b");

            await interactionRepository.AddAsync(interactionA);
            await interactionRepository.AddAsync(interactionB);
            await interactionRepository.SaveChangesAsync();

            var document = await SeedDocumentAsync(context, orgA.Id, userA.Id, "chat-citation.txt", now, DocumentProcessingStatus.Processed);
            var chunkId = Guid.NewGuid();
            context.DocumentChunks.Add(new DocumentChunk
            {
                Id = chunkId,
                DocumentId = document.Id,
                OrganizationId = orgA.Id,
                ChunkIndex = 0,
                Text = "Chunk text is stored but not returned by the repository contract.",
                CharacterLength = 58,
                TokenEstimate = 12,
                CreatedAt = now
            });
            await context.SaveChangesAsync();

            await citationRepository.AddRangeAsync([
                Citation.Create(interactionA.Id, orgA.Id, document.Id, chunkId, 2, "Policy", null, null, 0.8),
                Citation.Create(interactionA.Id, orgA.Id, document.Id, chunkId, 1, "Policy", 3, "Scope", 0.9)
            ]);
            await citationRepository.SaveChangesAsync();

            var orgAInteractions = await interactionRepository.GetBySessionIdAsync(sessionA.Id, orgA.Id);
            Assert.Equal([interactionA.Id], orgAInteractions.Select(i => i.Id).ToArray());
            Assert.Null(await interactionRepository.FindByIdAsync(interactionA.Id, orgB.Id));

            var orgACitations = await citationRepository.GetByInteractionIdAsync(interactionA.Id, orgA.Id);
            Assert.Equal([1, 2], orgACitations.Select(c => c.Rank).ToArray());
            Assert.Empty(await citationRepository.GetByInteractionIdAsync(interactionA.Id, orgB.Id));
        });
    }

    [SqlServerFact]
    public async Task DashboardRepository_AggregatesOnlyRequestedOrganizationRows()
    {
        await RunInDatabaseAsync(async (provider, context) =>
        {
            var dashboardRepository = provider.GetRequiredService<IDashboardRepository>();
            var now = DateTimeOffset.UtcNow;
            var orgA = CreateOrganization("Dashboard Org A", now);
            var orgB = CreateOrganization("Dashboard Org B", now);
            var userA1 = CreateUser(orgA.Id, $"dash-a1-{Guid.NewGuid():N}@example.test", now);
            var userA2 = CreateUser(orgA.Id, $"dash-a2-{Guid.NewGuid():N}@example.test", now);
            var userB = CreateUser(orgB.Id, $"dash-b-{Guid.NewGuid():N}@example.test", now);
            context.AddRange(orgA, orgB, userA1, userA2, userB);
            await context.SaveChangesAsync();

            await SeedDocumentAsync(context, orgA.Id, userA1.Id, "processed.txt", now, DocumentProcessingStatus.Processed);
            await SeedDocumentAsync(context, orgA.Id, userA1.Id, "failed.txt", now, DocumentProcessingStatus.Failed);
            await SeedDocumentAsync(context, orgB.Id, userB.Id, "other.txt", now, DocumentProcessingStatus.Processed);

            var grounded = await SeedInteractionAsync(context, orgA, userA1, now, AnswerState.Grounded);
            grounded.RecordGroundedOutcome("Answer.", Guid.NewGuid(), 1, 10, 20, 30, 100, 50, 0.01m, "Fake", "fake");
            var insufficient = await SeedInteractionAsync(context, orgA, userA2, now, AnswerState.InsufficientContext);
            insufficient.RecordInsufficientContextOutcome(Guid.NewGuid(), 0, 5, 5);
            var orgBInteraction = await SeedInteractionAsync(context, orgB, userB, now, AnswerState.ProviderFailed);
            orgBInteraction.RecordProviderFailedOutcome("ProviderUnavailable", Guid.NewGuid(), 0, 7, 8, 15);

            context.AnswerFeedback.AddRange(
                AnswerFeedback.Create(orgA.Id, userA1.Id, grounded.Id, AnswerFeedbackRating.Useful),
                AnswerFeedback.Create(orgA.Id, userA2.Id, insufficient.Id, AnswerFeedbackRating.NotUseful),
                AnswerFeedback.Create(orgB.Id, userB.Id, orgBInteraction.Id, AnswerFeedbackRating.NotUseful));
            await context.SaveChangesAsync();

            var range = DashboardDateRange.Create(
                now.AddMinutes(-10).UtcDateTime,
                now.AddMinutes(10).UtcDateTime);
            var overview = await dashboardRepository.GetOverviewAsync(orgA.Id, range);
            var chat = await dashboardRepository.GetChatAsync(orgA.Id, range);
            var documents = await dashboardRepository.GetDocumentsAsync(orgA.Id, range);
            var feedback = await dashboardRepository.GetFeedbackAsync(orgA.Id, range);

            Assert.Equal(2, overview.QuestionsAsked);
            Assert.Equal(2, overview.ActiveUsers);
            Assert.Equal(2, overview.DocumentsUploaded);
            Assert.Equal(1, overview.DocumentsProcessed);
            Assert.Equal(1, overview.DocumentsFailed);
            Assert.Equal(1, overview.InsufficientContextCount);
            Assert.Equal(0, overview.ProviderFailureCount);
            Assert.Equal(1, overview.UsefulFeedbackCount);
            Assert.Equal(1, overview.NotUsefulFeedbackCount);
            Assert.True(overview.EstimatedCostAvailable);
            Assert.Equal(0.01m, overview.EstimatedCostTotal);

            Assert.Equal(2, chat.QuestionsAsked);
            Assert.Equal(100L, chat.TokenInputTotal);
            Assert.Equal(50L, chat.TokenOutputTotal);
            Assert.Equal(0.01m, chat.EstimatedCostTotal);
            Assert.Equal(1, documents.Processed);
            Assert.Equal(1, documents.Failed);
            Assert.Equal(1, feedback.Useful);
            Assert.Equal(1, feedback.NotUseful);
        });
    }

    [SqlServerFact]
    public async Task AuditLogRepository_FiltersByOrganizationEventTypeDateAndLimit()
    {
        await RunInDatabaseAsync(async (provider, context) =>
        {
            var auditRepository = provider.GetRequiredService<IAuditLogRepository>();
            var orgA = Guid.NewGuid();
            var orgB = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            context.AuditLogEntries.AddRange(
                CreateAuditEntry(orgA, "DocumentUploadAccepted", "Included latest", now),
                CreateAuditEntry(orgA, "DocumentUploadAccepted", "Included older", now.AddMinutes(-1)),
                CreateAuditEntry(orgA, "DocumentProcessingFailed", "Wrong type", now),
                CreateAuditEntry(orgB, "DocumentUploadAccepted", "Wrong org", now));
            await context.SaveChangesAsync();

            var rows = await auditRepository.FindAsync(
                orgA,
                new AuditLogQuery(now.AddMinutes(-2), now.AddMinutes(1), "DocumentUploadAccepted"),
                limit: 1);

            var row = Assert.Single(rows);
            Assert.Equal("Included latest", row.Message);
            Assert.DoesNotContain(rows, r => r.Message == "Wrong org");
            Assert.DoesNotContain(rows, r => r.Message == "Wrong type");
        });
    }

    [SqlServerFact]
    public async Task DocumentProcessingRepository_CompletesDeterministicLifecycleAndScopesFailures()
    {
        await RunInDatabaseAsync(async (provider, context) =>
        {
            var documentRepository = provider.GetRequiredService<IDocumentRepository>();
            var now = DateTimeOffset.UtcNow;
            var orgA = CreateOrganization("Processing Org A", now);
            var orgB = CreateOrganization("Processing Org B", now);
            var userA = CreateUser(orgA.Id, $"processing-a-{Guid.NewGuid():N}@example.test", now);
            var userB = CreateUser(orgB.Id, $"processing-b-{Guid.NewGuid():N}@example.test", now);
            context.AddRange(orgA, orgB, userA, userB);
            await context.SaveChangesAsync();
            var docA = await SeedDocumentAsync(context, orgA.Id, userA.Id, "claim-me.txt", now, DocumentProcessingStatus.Uploaded);
            await SeedDocumentAsync(context, orgB.Id, userB.Id, "other-org-failed.txt", now, DocumentProcessingStatus.Failed);

            var claimed = await documentRepository.ClaimForProcessingAsync(docA.Id, now.AddSeconds(1));
            Assert.NotNull(claimed);
            Assert.Equal(DocumentProcessingStatus.Processing, claimed.ProcessingStatus);

            var failed = await documentRepository.MarkFailedAsync(
                docA.Id,
                "Unsupported document encoding.",
                now.AddSeconds(2));
            Assert.NotNull(failed);
            Assert.Equal(DocumentProcessingStatus.Failed, failed.ProcessingStatus);
            Assert.Equal("Unsupported document encoding.", failed.FailureReason);

            var orgAFailures = await documentRepository.FindFailedDocumentsAsync(orgA.Id, limit: 10);
            Assert.Equal([docA.Id], orgAFailures.Select(d => d.DocumentId).ToArray());
        });
    }

    private static async Task RunInDatabaseAsync(Func<ServiceProvider, KnowledgeOpsDbContext, Task> test)
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
        var databaseName = $"KnowledgeOpsIssue46_{Guid.NewGuid():N}";
        var databaseConnectionString = WithDatabase(baseConnectionString, databaseName);

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = databaseConnectionString
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);

            var provider = services.BuildServiceProvider();
            await using var _ = provider;
            var context = provider.GetRequiredService<KnowledgeOpsDbContext>();
            await context.Database.MigrateAsync();

            await test(provider, context);
        }
        finally
        {
            await DropDatabaseAsync(baseConnectionString, databaseName);
        }
    }

    private static Organization CreateOrganization(string name, DateTimeOffset timestamp) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Status = OrganizationStatus.Active,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

    private static User CreateUser(Guid organizationId, string email, DateTimeOffset timestamp) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            DisplayName = email,
            Email = email,
            Status = UserStatus.Active,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

    private static async Task<ChatInteraction> SeedInteractionAsync(
        KnowledgeOpsDbContext context,
        Organization organization,
        User user,
        DateTimeOffset timestamp,
        AnswerState? outcome = null)
    {
        if (!context.Organizations.Local.Any(o => o.Id == organization.Id)
            && !await context.Organizations.AnyAsync(o => o.Id == organization.Id))
        {
            context.Organizations.Add(organization);
        }

        if (!context.Users.Local.Any(u => u.Id == user.Id)
            && !await context.Users.AnyAsync(u => u.Id == user.Id))
        {
            context.Users.Add(user);
        }

        await context.SaveChangesAsync();

        var session = ChatSession.Create(organization.Id, user.Id, $"Session {Guid.NewGuid():N}");
        context.ChatSessions.Add(session);
        await context.SaveChangesAsync();

        var interaction = ChatInteraction.Create(
            session.Id,
            organization.Id,
            user.Id,
            "What is the policy?",
            $"hash-{Guid.NewGuid():N}",
            $"corr-{Guid.NewGuid():N}");

        if (outcome == AnswerState.InsufficientContext)
            interaction.RecordInsufficientContextOutcome(Guid.NewGuid(), 0, 1, 1);
        else if (outcome == AnswerState.ProviderFailed)
            interaction.RecordProviderFailedOutcome("ProviderUnavailable", Guid.NewGuid(), 0, 1, 1, 2);

        context.ChatInteractions.Add(interaction);
        await context.SaveChangesAsync();
        return interaction;
    }

    private static async Task<Document> SeedDocumentAsync(
        KnowledgeOpsDbContext context,
        Guid organizationId,
        Guid uploadedByUserId,
        string fileName,
        DateTimeOffset timestamp,
        DocumentProcessingStatus status)
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            FileName = fileName,
            Title = fileName,
            ContentType = "text/plain",
            FileSizeBytes = 42,
            StorageLocation = $"local://test/{fileName}",
            UploadedByUserId = uploadedByUserId,
            UploadedAt = timestamp,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        if (status is DocumentProcessingStatus.Processing or DocumentProcessingStatus.Processed or DocumentProcessingStatus.Failed)
            document.StartProcessing(timestamp.AddMilliseconds(1));
        if (status == DocumentProcessingStatus.Processed)
            document.MarkProcessed(timestamp.AddMilliseconds(2));
        if (status == DocumentProcessingStatus.Failed)
            document.MarkFailed("Safe test failure.", timestamp.AddMilliseconds(2));

        context.Documents.Add(document);
        await context.SaveChangesAsync();
        return document;
    }

    private static AuditLogEntry CreateAuditEntry(
        Guid organizationId,
        string eventType,
        string message,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventType = eventType,
            Message = message,
            Severity = AuditSeverity.Info,
            CorrelationId = "issue-46-test",
            CreatedAt = createdAt
        };

    private static string WithDatabase(string connectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = databaseName };
        return builder.ConnectionString;
    }

    private static async Task DropDatabaseAsync(string connectionString, string databaseName)
    {
        var masterConnectionString = WithDatabase(connectionString, "master");
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{databaseName}];
            END
            """;
        await command.ExecuteNonQueryAsync();
    }
}

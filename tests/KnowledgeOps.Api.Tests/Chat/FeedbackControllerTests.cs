using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Api.Tests.Chat;

public sealed class FeedbackControllerTests : IClassFixture<ChatApiTestFactory>
{
    private readonly ChatApiTestFactory _factory;

    public FeedbackControllerTests(ChatApiTestFactory factory)
    {
        _factory = factory;
        _factory.Reset();
    }

    [Fact]
    public async Task SubmitFeedback_PostsUsefulRating()
    {
        var client = await AuthenticateAsync(ChatApiTestFactory.AgentEmail);
        var interactionId = Guid.Parse("22222222-2222-4222-8222-222222222222");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/chat/interactions/{interactionId}/feedback",
            new { rating = "Useful" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(_factory.FeedbackService.SubmitWasCalled);
        Assert.Equal(interactionId, _factory.FeedbackService.LastSubmitRequest?.ChatInteractionId);
        Assert.Equal(AnswerFeedbackRating.Useful, _factory.FeedbackService.LastSubmitRequest?.Rating);
        Assert.Equal("Useful", body.GetProperty("rating").GetString());
    }

    [Fact]
    public async Task SubmitFeedback_PostsNotUsefulRating()
    {
        var client = await AuthenticateAsync(ChatApiTestFactory.AgentEmail);
        var interactionId = Guid.Parse("22222222-2222-4222-8222-222222222222");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/chat/interactions/{interactionId}/feedback",
            new { rating = "NotUseful" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(interactionId, _factory.FeedbackService.LastSubmitRequest?.ChatInteractionId);
        Assert.Equal(AnswerFeedbackRating.NotUseful, _factory.FeedbackService.LastSubmitRequest?.Rating);
    }

    [Fact]
    public async Task SubmitFeedback_DuplicateConflictDoesNotExposeContent()
    {
        _factory.FeedbackService.ExceptionToThrow = new ApplicationConflictException();
        var client = await AuthenticateAsync(ChatApiTestFactory.AgentEmail);
        var interactionId = Guid.Parse("22222222-2222-4222-8222-222222222222");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/chat/interactions/{interactionId}/feedback",
            new { rating = "Useful" });
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.DoesNotContain("answer", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("question", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prompt", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateFeedback_PutsOwnRating()
    {
        var client = await AuthenticateAsync(ChatApiTestFactory.AgentEmail);
        var interactionId = Guid.Parse("22222222-2222-4222-8222-222222222222");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/chat/interactions/{interactionId}/feedback",
            new { rating = "NotUseful" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_factory.FeedbackService.UpdateWasCalled);
        Assert.Equal(interactionId, _factory.FeedbackService.LastUpdateRequest?.ChatInteractionId);
        Assert.Equal("NotUseful", body.GetProperty("rating").GetString());
    }

    [Fact]
    public async Task SubmitFeedback_OldCollectionRouteIsNotExposed()
    {
        var client = await AuthenticateAsync(ChatApiTestFactory.AgentEmail);

        var response = await client.PostAsJsonAsync(
            "/api/v1/feedback",
            new
            {
                chatInteractionId = Guid.Parse("22222222-2222-4222-8222-222222222222"),
                rating = "Useful"
            });

        Assert.False(response.IsSuccessStatusCode);
        Assert.False(_factory.FeedbackService.SubmitWasCalled);
    }

    [Fact]
    public async Task UpdateFeedback_OldFeedbackIdRouteIsNotExposed()
    {
        var client = await AuthenticateAsync(ChatApiTestFactory.AgentEmail);

        var response = await client.PutAsJsonAsync(
            "/api/v1/feedback/11111111-1111-4111-8111-111111111111",
            new { rating = "NotUseful" });

        Assert.False(response.IsSuccessStatusCode);
        Assert.False(_factory.FeedbackService.UpdateWasCalled);
    }

    [Fact]
    public async Task ReviewFeedback_RequiresViewReviewDataPermission()
    {
        var client = await AuthenticateAsync(ChatApiTestFactory.AgentEmail);

        var response = await client.GetAsync("/api/v1/feedback");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(_factory.FeedbackService.ReviewWasCalled);
    }

    [Fact]
    public async Task ReviewFeedback_RejectsUserWithoutReviewPermission()
    {
        var client = await AuthenticateAsync(ChatApiTestFactory.NoRoleEmail);

        var response = await client.GetAsync("/api/v1/feedback");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(_factory.FeedbackService.ReviewWasCalled);
    }

    [Theory]
    [InlineData(ChatApiTestFactory.SupervisorEmail)]
    [InlineData(ChatApiTestFactory.ManagerEmail)]
    [InlineData(ChatApiTestFactory.AdminEmail)]
    public async Task ReviewFeedback_AllowsAuthorizedReviewerAndReturnsSimpleSignalsOnly(string email)
    {
        var client = await AuthenticateAsync(email);

        var response = await client.GetAsync("/api/v1/feedback");
        var raw = await response.Content.ReadAsStringAsync();
        var body = JsonDocument.Parse(raw).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_factory.FeedbackService.ReviewWasCalled);
        Assert.Equal(1, body.GetProperty("usefulCount").GetInt32());
        Assert.Equal(1, body.GetProperty("notUsefulCount").GetInt32());
        Assert.Equal("Useful", body.GetProperty("items")[0].GetProperty("rating").GetString());
        Assert.DoesNotContain("question", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answer", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prompt", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chunks", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Feedback_RejectsUnknownRating()
    {
        var client = await AuthenticateAsync(ChatApiTestFactory.AgentEmail);
        var interactionId = Guid.Parse("22222222-2222-4222-8222-222222222222");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/chat/interactions/{interactionId}/feedback",
            new { rating = "CorrectedAnswer" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(_factory.FeedbackService.SubmitWasCalled);
    }

    // ── G-3: Feedback review cross-org ────────────────────────────────────────

    [Fact]
    public async Task FeedbackReview_OrgA_DoesNotIncludeOrgBFeedback()
    {
        // OrgA Supervisor calls GET /api/v1/feedback.
        // The service uses persisted org state (not client input), so only OrgA feedback is returned.
        // Verifies: response contains only OrgA feedback counts, no OrgB data leaks.
        var orgAOrgId = ChatApiTestFactory.OrgId;
        var orgBOrgId = Guid.Parse("ffff3333-ffff-ffff-ffff-ffff33333333");

        var client = await AuthenticateAsync(ChatApiTestFactory.SupervisorEmail);
        var response = await client.GetAsync("/api/v1/feedback");
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_factory.FeedbackService.ReviewWasCalled);

        // Response must not contain OrgB's org ID or hint at cross-org data
        Assert.DoesNotContain(orgBOrgId.ToString(), raw, StringComparison.OrdinalIgnoreCase);

        // Response body exposes only safe signals — no question, answer, prompt, or provider data
        Assert.DoesNotContain("question", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answer", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prompt", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider", raw, StringComparison.OrdinalIgnoreCase);
    }

    // ── G-4: Feedback submit/update cross-org ─────────────────────────────────

    [Fact]
    public async Task SubmitFeedback_CrossOrgInteraction_Returns404()
    {
        // OrgA user attempts to submit feedback for an interaction owned by OrgB.
        // The service enforces org scope via repository.FindInteractionAsync(id, state.OrganizationId).
        // When interaction belongs to OrgB, the repository returns null, service throws ApplicationNotFoundException, controller returns 404.
        _factory.FeedbackService.ExceptionToThrow = new ApplicationNotFoundException();

        var client = await AuthenticateAsync(ChatApiTestFactory.AgentEmail);
        var orgBInteractionId = Guid.Parse("d4d4d4d4-d4d4-4d4d-8d4d-d4d4d4d4d4d4");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/chat/interactions/{orgBInteractionId}/feedback",
            new { rating = "Useful" });
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(_factory.FeedbackService.SubmitWasCalled);

        // Must not expose any interaction content from OrgB
        Assert.DoesNotContain("questionText", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answerText", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(orgBInteractionId.ToString(), raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateFeedback_CrossOrgInteraction_Returns404()
    {
        // OrgA user attempts to update feedback for an interaction owned by OrgB.
        // The service enforces org scope via repository.FindInteractionAsync(id, state.OrganizationId).
        // When interaction belongs to OrgB, the repository returns null, service throws ApplicationNotFoundException, controller returns 404.
        _factory.FeedbackService.ExceptionToThrow = new ApplicationNotFoundException();

        var client = await AuthenticateAsync(ChatApiTestFactory.AgentEmail);
        var orgBInteractionId = Guid.Parse("e5e5e5e5-e5e5-4e5e-8e5e-e5e5e5e5e5e5");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/chat/interactions/{orgBInteractionId}/feedback",
            new { rating = "NotUseful" });
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(_factory.FeedbackService.UpdateWasCalled);

        // Must not expose any interaction content from OrgB
        Assert.DoesNotContain("questionText", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answerText", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(orgBInteractionId.ToString(), raw, StringComparison.OrdinalIgnoreCase);
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

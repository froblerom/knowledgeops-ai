using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Chat.Citations;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Domain.Chat;
using Microsoft.Extensions.Logging;

namespace KnowledgeOps.Application.Chat;

internal sealed class ChatHistoryService(
    ICurrentUser currentUser,
    IUserAccessStateReader accessStateReader,
    IPermissionService permissionService,
    IChatSessionRepository sessionRepository,
    IChatInteractionRepository interactionRepository,
    ICitationRepository citationRepository,
    IAuditEventWriter auditWriter,
    ICorrelationContext correlationContext,
    ILogger<ChatHistoryService> logger) : IChatHistoryService
{
    private const int SessionListLimit = 50;

    public async Task<IReadOnlyList<ChatSessionSummaryDto>> GetSessionsAsync(
        bool scopedReview, CancellationToken ct = default)
    {
        var activeState = await RequireActiveStateAsync(ct);

        IReadOnlyList<ChatSession> sessions;

        if (scopedReview)
        {
            if (!permissionService.HasPermission(activeState, KnowledgeOpsPermissions.Chat.ViewScopedHistory))
            {
                await EmitDeniedAsync("GetSessions.ScopedReview", null, null, ct);
                return [];
            }

            sessions = await sessionRepository.GetRecentByOrganizationAsync(
                activeState.OrganizationId, SessionListLimit, ct);
        }
        else
        {
            sessions = await sessionRepository.GetRecentByUserAsync(
                activeState.UserId, activeState.OrganizationId, SessionListLimit, ct);
        }

        var result = new List<ChatSessionSummaryDto>(sessions.Count);
        foreach (var session in sessions)
        {
            var count = await sessionRepository.CountInteractionsBySessionAsync(session.Id, ct);
            result.Add(ToSummaryDto(session, count));
        }

        await EmitBestEffortAsync(new AuditEvent(
            AuditEventTypes.ChatHistoryViewed,
            "Chat session list accessed.",
            AuditSeverity.Info,
            correlationContext.CorrelationId,
            OrganizationId: activeState.OrganizationId,
            UserId: activeState.UserId,
            EntityType: "ChatSession"), ct);

        return result;
    }

    public async Task<Guid> CreateSessionAsync(string? title, CancellationToken ct = default)
    {
        var activeState = await RequireActiveStateAsync(ct);

        var session = ChatSession.Create(activeState.OrganizationId, activeState.UserId, title?.Trim());
        await sessionRepository.AddAsync(session, ct);
        await sessionRepository.SaveChangesAsync(ct);

        return session.Id;
    }

    public async Task<ChatSessionDetailDto?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var activeState = await RequireActiveStateAsync(ct);

        var session = await sessionRepository.FindByIdAndOrganizationAsync(
            sessionId, activeState.OrganizationId, ct);

        if (session is null)
            return null;

        if (!IsAuthorizedForResource(session.UserId, session.OrganizationId, activeState))
        {
            await EmitDeniedAsync("GetSession", "ChatSession", sessionId, ct);
            return null;
        }

        var interactions = await interactionRepository.GetBySessionIdAsync(
            session.Id, activeState.OrganizationId, ct);

        var interactionSummaries = interactions
            .Select(i => new ChatInteractionSummaryDto(
                i.Id,
                ToApiAnswerState(i.AnswerState),
                i.AnswerState == AnswerState.InsufficientContext,
                i.CreatedAt))
            .ToList();

        await EmitBestEffortAsync(new AuditEvent(
            AuditEventTypes.ChatHistoryViewed,
            "Chat session detail accessed.",
            AuditSeverity.Info,
            correlationContext.CorrelationId,
            OrganizationId: activeState.OrganizationId,
            UserId: activeState.UserId,
            EntityType: "ChatSession",
            EntityId: session.Id), ct);

        return new ChatSessionDetailDto(
            session.Id,
            session.Title,
            session.Status,
            session.CreatedAt,
            session.UpdatedAt,
            session.LastInteractionAt,
            interactionSummaries);
    }

    public async Task<ChatInteractionDetailDto?> GetInteractionAsync(
        Guid interactionId, CancellationToken ct = default)
    {
        var activeState = await RequireActiveStateAsync(ct);

        var interaction = await interactionRepository.FindByIdAsync(interactionId, activeState.OrganizationId, ct);

        if (interaction is null)
            return null;

        if (!IsAuthorizedForResource(interaction.UserId, interaction.OrganizationId, activeState))
        {
            await EmitDeniedAsync("GetInteraction", "ChatInteraction", interactionId, ct);
            return null;
        }

        var citations = await citationRepository.GetByInteractionIdAsync(
            interaction.Id, activeState.OrganizationId, ct);

        await EmitBestEffortAsync(new AuditEvent(
            AuditEventTypes.ChatInteractionViewed,
            "Chat interaction detail accessed.",
            AuditSeverity.Info,
            correlationContext.CorrelationId,
            OrganizationId: activeState.OrganizationId,
            UserId: activeState.UserId,
            EntityType: "ChatInteraction",
            EntityId: interaction.Id), ct);

        return new ChatInteractionDetailDto(
            interaction.Id,
            interaction.ChatSessionId,
            ToApiAnswerState(interaction.AnswerState),
            interaction.AnswerState == AnswerState.InsufficientContext,
            interaction.QuestionText,
            interaction.AnswerState == AnswerState.ProviderFailed ? null : interaction.AnswerText,
            interaction.PromptVersion,
            interaction.CorrelationId,
            new ChatRetrievalMetadataDto(
                interaction.RetrievalCandidateCount,
                interaction.RetrievalLatencyMs,
                interaction.GenerationLatencyMs,
                interaction.TotalLatencyMs,
                interaction.TokenUsageInput,
                interaction.TokenUsageOutput,
                interaction.EstimatedCost),
            citations.OrderBy(c => c.Rank).Select(ToCitationDto).ToList(),
            interaction.CreatedAt);
    }

    public async Task<IReadOnlyList<ChatCitationHistoryDto>?> GetInteractionCitationsAsync(
        Guid interactionId, CancellationToken ct = default)
    {
        var activeState = await RequireActiveStateAsync(ct);

        var interaction = await interactionRepository.FindByIdAsync(interactionId, activeState.OrganizationId, ct);

        if (interaction is null)
            return null;

        if (!IsAuthorizedForResource(interaction.UserId, interaction.OrganizationId, activeState))
        {
            await EmitDeniedAsync("GetInteractionCitations", "ChatInteraction", interactionId, ct);
            return null;
        }

        var citations = await citationRepository.GetByInteractionIdAsync(
            interaction.Id, activeState.OrganizationId, ct);

        return citations.OrderBy(c => c.Rank).Select(ToCitationDto).ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<UserAccessState> RequireActiveStateAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var state = await accessStateReader.FindActiveByIdAsync(currentUser.UserId, ct);
        if (state is null)
            throw new UnauthorizedAccessException("User is not active.");

        return state;
    }

    private bool IsAuthorizedForResource(Guid resourceUserId, Guid resourceOrgId, UserAccessState activeState)
    {
        if (resourceOrgId != activeState.OrganizationId)
            return false;

        if (resourceUserId == activeState.UserId)
            return true;

        // Scoped reviewer: Supervisor, Manager, Admin only via Chat.ViewScopedHistory.
        // KnowledgeAdmin and Agent are own-only.
        return permissionService.HasPermission(activeState, KnowledgeOpsPermissions.Chat.ViewScopedHistory);
    }

    private async Task EmitDeniedAsync(string action, string? entityType, Guid? entityId, CancellationToken ct)
    {
        await EmitBestEffortAsync(new AuditEvent(
            AuditEventTypes.ChatHistoryDenied,
            $"Chat history access denied for action {action}.",
            AuditSeverity.Warning,
            correlationContext.CorrelationId,
            OrganizationId: currentUser.IsAuthenticated ? currentUser.OrganizationId : null,
            UserId: currentUser.IsAuthenticated ? currentUser.UserId : null,
            EntityType: entityType,
            EntityId: entityId), ct);
    }

    private async Task EmitBestEffortAsync(AuditEvent auditEvent, CancellationToken ct)
    {
        try
        {
            await auditWriter.WriteAsync(auditEvent, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                "Audit write failed. EventType={EventType} CorrelationId={CorrelationId}",
                auditEvent.EventType,
                correlationContext.CorrelationId);
        }
    }

    private static string ToApiAnswerState(AnswerState state) => state switch
    {
        AnswerState.Grounded => "GroundedAnswer",
        AnswerState.InsufficientContext => "InsufficientContext",
        AnswerState.ProviderFailed => "ProviderFailure",
        _ => "ProviderFailure"
    };

    private static ChatSessionSummaryDto ToSummaryDto(ChatSession session, int interactionCount) =>
        new(session.Id, session.Title, session.Status, session.CreatedAt, session.UpdatedAt,
            session.LastInteractionAt, interactionCount);

    private static ChatCitationHistoryDto ToCitationDto(Citation c) =>
        new(c.Id, c.ChatInteractionId, c.DocumentId, c.ChunkId, c.Rank,
            c.DocumentTitle, c.PageNumber, c.SectionLabel, c.RelevanceScore);
}

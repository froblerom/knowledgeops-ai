namespace KnowledgeOps.Application.Chat;

public interface IChatHistoryService
{
    Task<IReadOnlyList<ChatSessionSummaryDto>> GetSessionsAsync(bool scopedReview, CancellationToken ct = default);
    Task<Guid> CreateSessionAsync(string? title, CancellationToken ct = default);
    Task<ChatSessionDetailDto?> GetSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<ChatInteractionDetailDto?> GetInteractionAsync(Guid interactionId, CancellationToken ct = default);
    Task<IReadOnlyList<ChatCitationHistoryDto>?> GetInteractionCitationsAsync(Guid interactionId, CancellationToken ct = default);
}

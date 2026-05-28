namespace KnowledgeOps.Application.Chat;

public interface IRagChatOrchestrationService
{
    Task<AskQuestionResponse> AskAsync(
        AskQuestionRequest request,
        CancellationToken cancellationToken = default);
}

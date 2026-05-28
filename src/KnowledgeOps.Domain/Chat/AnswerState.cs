namespace KnowledgeOps.Domain.Chat;

public enum AnswerState
{
    Grounded = 0,
    InsufficientContext = 1,
    ProviderFailed = 2,
}

namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class OpenAIAnswerGeneratorSettings
{
    public string ApiEndpoint { get; init; } = "https://api.openai.com/v1/chat/completions";
    public string Model { get; init; } = "gpt-4.1-mini";
    public int MaxTokens { get; init; } = 600;
    public double Temperature { get; init; } = 0.1;
    public int TimeoutSeconds { get; init; } = 30;
}

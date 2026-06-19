using KnowledgeOps.Application.Observability;

namespace KnowledgeOps.Infrastructure.Observability;

internal sealed class AiProviderDiagnosticsService(
    string answerProvider,
    bool openAiConfigured,
    string? model,
    string? localProviderBaseUrl = null) : IAiProviderDiagnostics
{
    public string AnswerProvider { get; } = answerProvider;
    public bool OpenAiConfigured { get; } = openAiConfigured;
    public string? Model { get; } = model;
    public string? LocalProviderBaseUrl { get; } = localProviderBaseUrl;
}

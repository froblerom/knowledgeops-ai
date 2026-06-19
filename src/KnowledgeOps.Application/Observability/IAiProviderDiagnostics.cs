namespace KnowledgeOps.Application.Observability;

/// <summary>
/// Read-only, sanitized view of the active AI answer provider for use in health diagnostics.
/// Never exposes API keys, partial key fragments, or raw configuration values.
/// </summary>
public interface IAiProviderDiagnostics
{
    string AnswerProvider { get; }
    bool OpenAiConfigured { get; }
    string? Model { get; }

    /// <summary>
    /// Base URL of the local OpenAI-compatible endpoint, if LocalOpenAICompatible provider is active.
    /// Null for Demo and OpenAI providers. Safe to expose to Admins — not a secret.
    /// </summary>
    string? LocalProviderBaseUrl { get; }
}

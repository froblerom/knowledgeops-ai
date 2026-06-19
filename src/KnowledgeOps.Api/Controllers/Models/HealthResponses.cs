namespace KnowledgeOps.Api.Controllers.Models;

public sealed record BasicHealthResponse(string Status, DateTimeOffset Timestamp);

public sealed record DetailedHealthResponse(
    string Status,
    HealthDependencyResponse Dependencies,
    AiProviderStatusResponse AiProvider,
    DateTimeOffset Timestamp);

public sealed record HealthDependencyResponse(string Application, string Database, string Retrieval);

/// <summary>
/// Sanitized AI provider status for Admin health diagnostics.
/// Never contains API keys, partial key fragments, or raw configuration values.
/// </summary>
public sealed record AiProviderStatusResponse(
    string AnswerProvider,
    bool OpenAiConfigured,
    string? Model,
    string? LocalProviderBaseUrl);

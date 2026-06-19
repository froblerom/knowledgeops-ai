namespace KnowledgeOps.Infrastructure.Chat;

internal sealed class LocalOpenAICompatibleAnswerGeneratorSettings
{
    public string ProviderName { get; init; } = "QwenLocal";
    public string BaseUrl { get; init; } = "http://localhost:11434/v1";

    /// <summary>
    /// Optional bearer token. Ollama does not require authentication.
    /// Set via user-secrets or environment variable if a specific runtime requires it.
    /// Never commit a real token to source control.
    /// </summary>
    public string? ApiKey { get; init; }

    public string Model { get; init; } = "qwen3:8b";

    /// <summary>
    /// Maximum tokens for the combined thinking + answer budget.
    /// Qwen3 and other reasoning models consume tokens in their internal CoT phase before
    /// producing visible content. 4000 provides sufficient headroom for RAG-style prompts
    /// (system instruction + context chunks + question) while staying well within the
    /// default TimeoutSeconds = 90 limit (~6–14 s measured on Qwen3:8b 4-bit local).
    /// Increase further if you use a larger model or need very long answers.
    /// </summary>
    public int MaxTokens { get; init; } = 4000;
    public double Temperature { get; init; } = 0.1;
    public int TimeoutSeconds { get; init; } = 90;

    /// <summary>
    /// When true, strips &lt;think&gt;…&lt;/think&gt; blocks from the model output before
    /// returning the answer. Applies to providers that embed reasoning as XML tags directly
    /// inside the content field (e.g., bare llama.cpp, some vLLM configurations).
    /// NOTE: Ollama's OpenAI-compatible endpoint (/v1/chat/completions) places reasoning
    /// in a separate "reasoning" field rather than inside content, so this setting has no
    /// effect when using Ollama — the content field received by this generator is already
    /// free of think tags.
    /// </summary>
    public bool StripThinking { get; init; } = true;
}

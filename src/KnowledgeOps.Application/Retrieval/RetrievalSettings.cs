namespace KnowledgeOps.Application.Retrieval;

public sealed class RetrievalSettings
{
    public const int FallbackDefaultTopK = 5;
    public const int FallbackMaxTopK = 20;

    public int DefaultTopK { get; init; } = FallbackDefaultTopK;
    public int MaxTopK { get; init; } = FallbackMaxTopK;

    public int NormalizeTopK(int requestedTopK)
    {
        var maxTopK = MaxTopK > 0 ? MaxTopK : FallbackMaxTopK;
        var defaultTopK = DefaultTopK > 0 ? DefaultTopK : FallbackDefaultTopK;

        if (defaultTopK > maxTopK)
            defaultTopK = maxTopK;

        var selectedTopK = requestedTopK <= 0 ? defaultTopK : requestedTopK;
        return selectedTopK > maxTopK ? maxTopK : selectedTopK;
    }
}

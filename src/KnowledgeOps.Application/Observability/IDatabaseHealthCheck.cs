namespace KnowledgeOps.Application.Observability;

public interface IDatabaseHealthCheck
{
    Task<DatabaseHealthResult> CheckAsync(CancellationToken ct = default);
}

public sealed record DatabaseHealthResult(bool IsHealthy)
{
    public static DatabaseHealthResult Healthy { get; } = new(true);
    public static DatabaseHealthResult Unavailable { get; } = new(false);
}

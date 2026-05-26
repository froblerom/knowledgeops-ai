namespace KnowledgeOps.Worker;

public sealed class WorkerSettings
{
    public int PollingIntervalSeconds { get; init; } = 10;

    public int EffectivePollingIntervalSeconds =>
        PollingIntervalSeconds <= 0 ? 10 : PollingIntervalSeconds;
}

using KnowledgeOps.Application.Observability;

namespace KnowledgeOps.Worker;

/// <summary>
/// Provides a stable correlation ID for the lifetime of a single processing scope.
/// Each BackgroundService tick creates a new DI scope, producing a new correlation ID.
/// </summary>
internal sealed class WorkerCorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; } = CorrelationIdPolicy.AcceptOrCreate(null);
}

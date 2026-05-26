namespace KnowledgeOps.Application.Observability;

public interface ICorrelationContext
{
    string CorrelationId { get; }
}

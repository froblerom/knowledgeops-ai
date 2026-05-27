namespace KnowledgeOps.Application.Retrieval;

public interface IRetrievalStorageHealthCheck
{
    Task<RetrievalStorageHealthResult> CheckAsync(CancellationToken cancellationToken = default);
}

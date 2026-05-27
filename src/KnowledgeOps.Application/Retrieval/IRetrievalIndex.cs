namespace KnowledgeOps.Application.Retrieval;

public interface IRetrievalIndex
{
    Task<VectorIndexResult> IndexAsync(
        VectorIndexRequest request,
        CancellationToken cancellationToken = default);
}

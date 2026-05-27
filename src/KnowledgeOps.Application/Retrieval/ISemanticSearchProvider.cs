namespace KnowledgeOps.Application.Retrieval;

public interface ISemanticSearchProvider
{
    Task<SemanticQueryResult> SearchAsync(
        SemanticQueryRequest request,
        CancellationToken cancellationToken = default);
}

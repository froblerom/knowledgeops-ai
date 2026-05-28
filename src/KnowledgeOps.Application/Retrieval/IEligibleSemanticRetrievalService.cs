namespace KnowledgeOps.Application.Retrieval;

public interface IEligibleSemanticRetrievalService
{
    Task<EligibleSemanticRetrievalResult> RetrieveAsync(
        EligibleSemanticRetrievalRequest request,
        CancellationToken cancellationToken = default);
}

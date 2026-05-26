namespace KnowledgeOps.Application.Documents;

public interface IDocumentProcessingOrchestrator
{
    /// <summary>
    /// Claims and processes one pending uploaded document.
    /// Returns true if a document was claimed (regardless of success/failure outcome),
    /// or false if no pending documents were available.
    /// </summary>
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default);
}

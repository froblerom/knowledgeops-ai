namespace KnowledgeOps.Application.Documents;

/// <summary>
/// A single processing step executed against a claimed document.
/// Sprint 13: extraction and chunking step is implemented.
/// Future sprints may attach embedding steps here.
/// </summary>
public interface IDocumentProcessingStep
{
    Task ExecuteAsync(ManagedDocument document, CancellationToken cancellationToken = default);
}

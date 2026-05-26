namespace KnowledgeOps.Application.Documents;

/// <summary>
/// A single processing step executed against a claimed document.
/// Sprint 12: only a placeholder step is implemented.
/// Future sprints attach extraction, chunking, and embedding steps here.
/// </summary>
public interface IDocumentProcessingStep
{
    Task ExecuteAsync(ManagedDocument document, CancellationToken cancellationToken = default);
}

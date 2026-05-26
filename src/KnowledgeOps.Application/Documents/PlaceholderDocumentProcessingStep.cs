namespace KnowledgeOps.Application.Documents;

/// <summary>
/// Sprint 12 placeholder: succeeds immediately without reading file content,
/// parsing documents, extracting text, creating chunks, generating embeddings,
/// performing retrieval, or calling any AI provider.
/// Future sprints replace or extend this with real extraction and embedding steps.
/// </summary>
public sealed class PlaceholderDocumentProcessingStep : IDocumentProcessingStep
{
    public Task ExecuteAsync(ManagedDocument document, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

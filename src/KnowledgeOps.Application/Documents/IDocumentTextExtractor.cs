namespace KnowledgeOps.Application.Documents;

public interface IDocumentTextExtractor
{
    bool Supports(string contentType);

    Task<string> ExtractAsync(Stream fileStream, CancellationToken cancellationToken = default);
}

using KnowledgeOps.Application.Documents;

namespace KnowledgeOps.Infrastructure.Documents;

internal sealed class TxtMarkdownTextExtractor : IDocumentTextExtractor
{
    private static readonly HashSet<string> SupportedBaseTypes =
    [
        "text/plain",
        "text/markdown"
    ];

    public bool Supports(string contentType)
    {
        var baseType = ParseBaseType(contentType);
        return SupportedBaseTypes.Contains(baseType);
    }

    public async Task<string> ExtractAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(fileStream, leaveOpen: true);
            var text = await reader.ReadToEndAsync(cancellationToken);
            return NormalizeLineEndings(text);
        }
        catch (DocumentExtractionException)
        {
            throw;
        }
        catch
        {
            throw new DocumentExtractionException("Document text extraction failed.");
        }
    }

    private static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n").Replace("\r", "\n");

    private static string ParseBaseType(string contentType)
    {
        var semicolon = contentType.IndexOf(';');
        return (semicolon < 0 ? contentType : contentType[..semicolon]).Trim().ToLowerInvariant();
    }
}

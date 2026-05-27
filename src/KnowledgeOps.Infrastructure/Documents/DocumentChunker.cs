using KnowledgeOps.Application.Documents;

namespace KnowledgeOps.Infrastructure.Documents;

internal sealed class DocumentChunker : IDocumentChunker
{
    public const int MaxChunkCharacters = 1200;
    public const int OverlapCharacters = 150;

    public IReadOnlyList<TextChunk> Chunk(string text)
    {
        if (string.IsNullOrEmpty(text))
            throw new DocumentChunkingException("No usable text could be extracted from the document.");

        try
        {
            var chunks = new List<TextChunk>();
            var index = 0;
            var position = 0;

            while (position < text.Length)
            {
                var length = Math.Min(MaxChunkCharacters, text.Length - position);
                var slice = text.Substring(position, length).Trim();

                if (slice.Length > 0)
                {
                    var characterLength = slice.Length;
                    var tokenEstimate = (int)Math.Ceiling(characterLength / 4.0);
                    chunks.Add(new TextChunk(index, slice, characterLength, tokenEstimate));
                    index++;
                }

                if (position + length >= text.Length)
                    break;

                position += length - OverlapCharacters;
            }

            return chunks;
        }
        catch (DocumentChunkingException)
        {
            throw;
        }
        catch
        {
            throw new DocumentChunkingException("Document chunking failed.");
        }
    }
}

namespace KnowledgeOps.Application.Documents;

public sealed record TextChunk(int Index, string Text, int CharacterLength, int TokenEstimate);

public interface IDocumentChunker
{
    IReadOnlyList<TextChunk> Chunk(string text);
}

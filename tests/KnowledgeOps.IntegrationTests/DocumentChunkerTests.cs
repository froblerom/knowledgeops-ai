using KnowledgeOps.Application.Documents;
using KnowledgeOps.Infrastructure.Documents;

namespace KnowledgeOps.IntegrationTests;

public sealed class DocumentChunkerTests
{
    [Fact]
    public void Chunk_EmptyText_ThrowsDocumentChunkingException()
    {
        var chunker = new DocumentChunker();

        Assert.Throws<DocumentChunkingException>(() => chunker.Chunk(string.Empty));
    }

    [Fact]
    public void Chunk_ShortText_ProducesSingleChunk()
    {
        var chunker = new DocumentChunker();
        var text = new string('a', 100);

        var chunks = chunker.Chunk(text);

        Assert.Single(chunks);
        Assert.Equal(0, chunks[0].Index);
        Assert.Equal(text, chunks[0].Text);
        Assert.Equal(100, chunks[0].CharacterLength);
    }

    [Fact]
    public void Chunk_TextExceedingMax_ProducesMultipleChunksWithOverlap()
    {
        var chunker = new DocumentChunker();
        var text = new string('x', DocumentChunker.MaxChunkCharacters + 100);

        var chunks = chunker.Chunk(text);

        Assert.True(chunks.Count >= 2);
        Assert.Equal(DocumentChunker.MaxChunkCharacters, chunks[0].CharacterLength);

        // Overlap: last `OverlapCharacters` of chunk 0 should equal first `OverlapCharacters` of chunk 1
        var endOfFirst = chunks[0].Text[^DocumentChunker.OverlapCharacters..];
        var startOfSecond = chunks[1].Text[..DocumentChunker.OverlapCharacters];
        Assert.Equal(endOfFirst, startOfSecond);
    }

    [Fact]
    public void Chunk_TokenEstimateIsCeilingOfCharLengthDividedByFour()
    {
        var chunker = new DocumentChunker();
        var text = new string('a', 10);

        var chunks = chunker.Chunk(text);

        Assert.Single(chunks);
        Assert.Equal((int)Math.Ceiling(10 / 4.0), chunks[0].TokenEstimate);
    }

    [Fact]
    public void Chunk_ChunkIndicesAreSequential()
    {
        var chunker = new DocumentChunker();
        var text = new string('z', DocumentChunker.MaxChunkCharacters * 3);

        var chunks = chunker.Chunk(text);

        for (var i = 0; i < chunks.Count; i++)
            Assert.Equal(i, chunks[i].Index);
    }

    [Fact]
    public void Chunk_TextExactlyMaxLength_ProducesSingleChunk()
    {
        var chunker = new DocumentChunker();
        var text = new string('a', DocumentChunker.MaxChunkCharacters);

        var chunks = chunker.Chunk(text);

        Assert.Single(chunks);
    }

    [Fact]
    public void Chunk_TrimsIndividualChunks()
    {
        var chunker = new DocumentChunker();
        var text = "  hello world  ";

        var chunks = chunker.Chunk(text);

        Assert.All(chunks, c =>
        {
            Assert.Equal(c.Text, c.Text.Trim());
            Assert.Equal(c.CharacterLength, c.Text.Length);
        });
    }

    [Fact]
    public void Chunk_WhitespaceOnlySlice_IsSkipped()
    {
        var chunker = new DocumentChunker();
        // Position the window so one slide is entirely whitespace.
        // Stride = MaxChunkCharacters - OverlapCharacters.
        var stride = DocumentChunker.MaxChunkCharacters - DocumentChunker.OverlapCharacters;
        var text = "A" + new string(' ', DocumentChunker.MaxChunkCharacters + stride) + "B";

        var chunks = chunker.Chunk(text);

        Assert.All(chunks, c => Assert.False(string.IsNullOrWhiteSpace(c.Text)));
        // Both non-whitespace anchors must appear.
        Assert.Contains(chunks, c => c.Text.Contains('A'));
        Assert.Contains(chunks, c => c.Text.Contains('B'));
    }
}

using KnowledgeOps.Infrastructure.Retrieval;

namespace KnowledgeOps.IntegrationTests;

public sealed class LocalVectorStoreAlgorithmTests
{
    [Fact]
    public void TryParseVector_HandlesMalformedVectorDataSafely()
    {
        Assert.False(LocalVectorStore.TryParseVector("not-json", out var vector));
        Assert.Empty(vector);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("[null]")]
    public void TryParseVector_HandlesEmptyOrInvalidVectorDataSafely(string vectorData)
    {
        Assert.False(LocalVectorStore.TryParseVector(vectorData, out _));
    }

    [Fact]
    public void TryCosineSimilarity_ReturnsDeterministicCosineScore()
    {
        var success = LocalVectorStore.TryCosineSimilarity(
            [1, 0],
            [1, 0],
            out var score);

        Assert.True(success);
        Assert.Equal(1, score, precision: 6);
    }

    [Fact]
    public void TryCosineSimilarity_HandlesDimensionMismatchSafely()
    {
        var success = LocalVectorStore.TryCosineSimilarity(
            [1, 0],
            [1, 0, 0],
            out _);

        Assert.False(success);
    }

    [Fact]
    public void TryCosineSimilarity_HandlesZeroNormVectorSafely()
    {
        var success = LocalVectorStore.TryCosineSimilarity(
            [1, 0],
            [0, 0],
            out _);

        Assert.False(success);
    }
}

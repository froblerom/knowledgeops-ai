using KnowledgeOps.Application.Embeddings;
using KnowledgeOps.Infrastructure.Embeddings;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.IntegrationTests;

public sealed class FakeEmbeddingProviderTests
{
    private static FakeEmbeddingProvider BuildProvider(
        string providerName = "Fake",
        string modelName = "fake-deterministic-v1",
        int dimensions = 16) =>
        new(Options.Create(new FakeEmbeddingProviderSettings
        {
            ProviderName = providerName,
            ModelName = modelName,
            Dimensions = dimensions
        }));

    [Fact]
    public async Task GenerateAsync_SameInput_ReturnsDeterministicVector()
    {
        var provider = BuildProvider();
        var request = new EmbeddingRequest("Hello world.", "fake-deterministic-v1", 16);

        var first = await provider.GenerateAsync(request);
        var second = await provider.GenerateAsync(request);

        Assert.Equal(first.Vector, second.Vector);
    }

    [Fact]
    public async Task GenerateAsync_DifferentText_ReturnsDifferentVectors()
    {
        var provider = BuildProvider();
        var requestA = new EmbeddingRequest("Hello world.", "fake-deterministic-v1", 16);
        var requestB = new EmbeddingRequest("Goodbye world.", "fake-deterministic-v1", 16);

        var a = await provider.GenerateAsync(requestA);
        var b = await provider.GenerateAsync(requestB);

        Assert.NotEqual(a.Vector, b.Vector);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsVectorWithRequestedDimensions()
    {
        var provider = BuildProvider(dimensions: 8);
        var request = new EmbeddingRequest("Test text.", "fake-deterministic-v1", 8);

        var response = await provider.GenerateAsync(request);

        Assert.Equal(8, response.Vector.Length);
    }

    [Fact]
    public async Task GenerateAsync_VectorValuesAreInNormalizedRange()
    {
        var provider = BuildProvider();
        var request = new EmbeddingRequest("Range check.", "fake-deterministic-v1", 16);

        var response = await provider.GenerateAsync(request);

        Assert.All(response.Vector, v => Assert.InRange(v, -1.0f, 1.0f));
    }

    [Fact]
    public async Task GenerateAsync_DifferentModels_ReturnsDifferentVectors()
    {
        var providerA = BuildProvider(modelName: "model-a");
        var providerB = BuildProvider(modelName: "model-b");
        var request = new EmbeddingRequest("Same text.", "model-a", 16);
        var requestB = new EmbeddingRequest("Same text.", "model-b", 16);

        var a = await providerA.GenerateAsync(request);
        var b = await providerB.GenerateAsync(requestB);

        Assert.NotEqual(a.Vector, b.Vector);
    }
}

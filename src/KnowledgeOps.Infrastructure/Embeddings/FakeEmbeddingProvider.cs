using System.Security.Cryptography;
using System.Text;
using KnowledgeOps.Application.Embeddings;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.Infrastructure.Embeddings;

internal sealed class FakeEmbeddingProvider(IOptions<FakeEmbeddingProviderSettings> options) : IEmbeddingProvider
{
    private readonly FakeEmbeddingProviderSettings _settings = options.Value;

    public string ProviderName => _settings.ProviderName;
    public string DefaultModelName => _settings.ModelName;
    public int DefaultDimensions => _settings.Dimensions;

    public Task<EmbeddingResponse> GenerateAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        var dimensions = request.Dimensions > 0 ? request.Dimensions : _settings.Dimensions;
        var vector = GenerateDeterministicVector(request.Text, request.ModelName, dimensions);
        return Task.FromResult(new EmbeddingResponse(vector));
    }

    private static float[] GenerateDeterministicVector(string text, string modelName, int dimensions)
    {
        // Seed from SHA-256 so the result is deterministic and runtime-hash-independent.
        var input = Encoding.UTF8.GetBytes($"{modelName}|{dimensions}|{text}");
        var hash = SHA256.HashData(input);

        var vector = new float[dimensions];
        for (var i = 0; i < dimensions; i++)
        {
            // Map each 4-byte block of the hash (cycling) to a float in [-1, 1].
            var offset = (i * 4) % (hash.Length - 3);
            var raw = BitConverter.ToInt32(hash, offset);
            vector[i] = raw / (float)int.MaxValue;
        }

        return vector;
    }
}

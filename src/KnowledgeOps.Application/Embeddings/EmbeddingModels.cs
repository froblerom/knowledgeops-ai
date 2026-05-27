namespace KnowledgeOps.Application.Embeddings;

public sealed record EmbeddingRequest(string Text, string ModelName, int Dimensions);

public sealed record EmbeddingResponse(float[] Vector);

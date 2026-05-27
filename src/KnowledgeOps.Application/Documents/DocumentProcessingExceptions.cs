namespace KnowledgeOps.Application.Documents;

public sealed class DocumentExtractionException(string message) : Exception(message);

public sealed class DocumentChunkingException(string message) : Exception(message);

public sealed class DocumentEmbeddingException(string message) : Exception(message);

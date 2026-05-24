# ADR-007: Use RAG with Source Citations

## Status

Accepted

## Context

KnowledgeOps-AI is not intended to be a generic chatbot.

The business problem is that contact center users need fast, reliable, and traceable access to internal knowledge.

A generic LLM response is not sufficient because it may not know internal policies, may hallucinate, and may not provide source traceability.

The system must answer questions using approved internal documents and provide citations when answers are grounded in retrieved sources.

## Decision

KnowledgeOps-AI will use **Retrieval-Augmented Generation** with source citations as the core AI pattern.

The RAG flow will:

1. Receive a user question.
2. Validate user identity, role, and organization scope.
3. Retrieve authorized relevant chunks.
4. Detect insufficient context.
5. Build a grounded prompt with retrieved context.
6. Generate an AI answer.
7. Map retrieved chunks to citations.
8. Return answer and citations.
9. Store chat, retrieval, citation, latency, cost, and feedback metadata.

## Consequences

Positive consequences:

- Answers are grounded in internal documents.
- Citations improve trust.
- Retrieval metadata supports review.
- Insufficient-context behavior reduces hallucination risk.
- The project demonstrates applied AI beyond a simple chatbot.
- Supports business and portfolio value.

Negative consequences:

- RAG quality depends on document quality, chunking, embeddings, and retrieval.
- Citation mapping adds implementation complexity.
- Prompt design must be handled carefully.
- Retrieval latency and model latency affect user experience.
- Cost tracking becomes important.

## Alternatives Considered

### Generic Chatbot Without Retrieval

Rejected because it would not solve the internal knowledge problem safely.

### Fine-Tuned Model

Rejected for MVP because fine-tuning is unnecessary for document-grounded knowledge retrieval and adds complexity.

### Keyword Search Only

Rejected because semantic retrieval is central to the AI-assisted knowledge experience, though keyword or hybrid search may be added later.
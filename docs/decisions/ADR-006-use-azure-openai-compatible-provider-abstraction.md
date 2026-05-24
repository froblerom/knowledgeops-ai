# ADR-006: Use Azure OpenAI-Compatible Provider Abstraction

## Status

Accepted

## Context

KnowledgeOps-AI requires AI capabilities for:

- Embedding generation.
- RAG answer generation.
- Prompt execution.
- Usage metadata such as latency, token usage, and estimated cost where available.

Azure OpenAI is a strong fit for enterprise AI solutions, especially in Microsoft-oriented environments. However, the project should not tightly couple business logic to one provider SDK.

The system may use Azure OpenAI, OpenAI API, or compatible local/fake providers for testing and development.

## Decision

KnowledgeOps-AI will use provider abstractions for AI capabilities.

Recommended abstractions include:

```text
IEmbeddingProvider
IAiAnswerGenerator
ICostEstimator
```

Provider implementations may include:

```text
AzureOpenAIEmbeddingProvider
AzureOpenAIAnswerGenerator
OpenAIEmbeddingProvider
OpenAIAnswerGenerator
FakeEmbeddingProvider
FakeAiAnswerGenerator
```

Application and Domain layers must not depend directly on provider SDK types.

## Consequences

Positive consequences:

- Easier testing without live AI calls.
- Provider implementation can change with less impact.
- Supports Azure-ready architecture.
- Reduces vendor lock-in.
- Keeps business logic stable.
- Supports fake providers in CI.

Negative consequences:

- Requires adapter code.
- Some provider-specific features may be abstracted away.
- Metadata mapping must be designed carefully.
- Multiple provider implementations may require additional configuration.

## Alternatives Considered

### Direct Azure OpenAI SDK Usage in Application Services

Rejected because it would couple use case orchestration to provider SDKs and make tests harder.

### OpenAI API Only

Rejected because Azure OpenAI better supports enterprise positioning and Azure-ready design.

### Local Model Only

Rejected for MVP because the project aims to demonstrate cloud-ready applied AI integration.
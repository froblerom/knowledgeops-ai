# ADR-008: Use Asynchronous Document Processing

## Status

Accepted

## Context

KnowledgeOps-AI must process uploaded documents before they can be used for retrieval.

Document processing includes:

- Storing uploaded files.
- Extracting text.
- Splitting text into chunks.
- Generating embeddings.
- Storing vector data or vector references.
- Updating processing status.
- Recording failures.

This workflow can be slow and may depend on external providers.

If document upload waited for the full processing workflow to complete, the user experience would be slower and less reliable.

## Decision

KnowledgeOps-AI will process uploaded documents asynchronously.

The upload endpoint will accept a valid document, store the file and metadata, set initial status to `Uploaded`, and schedule or expose it for background processing.

A background worker will process the document and update status to:

- Processing
- Processed
- Failed

## Consequences

Positive consequences:

- Upload requests remain responsive.
- Long-running ingestion is isolated from request handling.
- Processing status becomes visible to users.
- Failures can be recorded and reviewed.
- The architecture is more production-realistic.

Negative consequences:

- Requires background worker design.
- Requires document status tracking.
- Requires retry or failure handling strategy.
- Users must understand that uploaded documents are not immediately searchable.
- Testing must cover asynchronous lifecycle behavior.

## Alternatives Considered

### Synchronous Processing During Upload

Rejected because document extraction and embedding generation may be slow and provider-dependent.

### Manual Offline Processing

Rejected because it would reduce the product value and make the MVP less realistic.

### External Queue-First Architecture

A full queue-based architecture was considered but may be deferred if MVP can start with a simpler background service.
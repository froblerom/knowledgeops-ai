# ADR-009: Use Mermaid for Architecture Diagrams

## Status

Accepted

## Context

KnowledgeOps-AI requires diagrams for business process flows, C4 architecture, UML diagrams, domain relationships, and database ERD.

The project documentation should be versionable, readable in Markdown, friendly to pull request review, and useful for AI coding agents.

Binary diagram files are useful for presentation, but they are harder to diff and maintain as the source of truth.

## Decision

KnowledgeOps-AI will use **Mermaid** as the source format for diagrams in Markdown documentation.

Rendered PNG files may be generated as documentation artifacts when needed.

Recommended rendered diagram folders include:

```text
docs/diagrams/architecture/
docs/diagrams/uml/
docs/diagrams/database/
docs/diagrams/business-process/
```

The Markdown Mermaid diagrams remain the source of truth.

## Consequences

Positive consequences:

- Diagrams are versionable.
- Diagrams can be reviewed in pull requests.
- Diagrams are readable by AI agents.
- GitHub and many documentation tools can render Mermaid.
- PNG artifacts can be generated later.
- Documentation remains close to the repository.

Negative consequences:

- Mermaid has layout limitations.
- Complex diagrams may become crowded.
- Some advanced UML/C4 notation may not be perfectly represented.
- Rendering can vary slightly across tools.

## Alternatives Considered

### Draw.io / diagrams.net

Good for manual diagramming but harder to review as source text.

### PlantUML

Powerful and text-based, but Mermaid is simpler for GitHub-centered Markdown documentation.

### PNG-Only Diagrams

Rejected because binary images are not a good source of truth for evolving architecture documentation.

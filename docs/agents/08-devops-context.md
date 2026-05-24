# DevOps Context

## MVP Delivery Posture

- Support reproducible local development with Docker and local SQL Server.
- Use GitHub Actions for build and test validation when implementation exists.
- Keep the architecture Azure-ready without requiring production cloud delivery in MVP.
- Use fictional or synthetic development/test data only.

## Environment And Secrets

- Keep environment-specific configuration external to source code.
- Do not commit provider keys, tokens, passwords, sensitive connection strings or production configuration.
- Do not expose secrets through logs, health output, CI artifacts or documentation examples.
- Configure fake AI providers for normal automated testing and CI.

## CI Expectations

- Validate relevant backend build/tests and Angular build/tests when those projects exist.
- Validate Docker/container behavior when changed and practical.
- Use deterministic fakes for embedding and answer generation.
- Normal CI must not depend on live AI-provider availability or cost.

## Database And Rollback Awareness

- Review migrations before merge and validate SQL Server migration changes locally when present.
- Treat database rollback carefully because historical documents, chats, citations, feedback and audit traceability must be preserved.
- Document risky deployment/configuration changes and rollback considerations.

## Azure-Ready Boundary

Future adapters may support Azure hosting, SQL, storage, retrieval, secrets and telemetry. Full production provisioning, hardening and operational rollout are not MVP requirements.

## Sources

- `docs/18-deployment-and-devops.md`
- `docs/17-testing-strategy.md`
- `docs/19-observability-and-support.md`
- `docs/21-implementation-roadmap.md`
- `docs/22-implementation-guardrails.md`
- ADR-006


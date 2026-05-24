# Deployment and DevOps

## 1. Purpose

This document defines how **KnowledgeOps-AI** moves from local development to deployable software.

KnowledgeOps-AI is an enterprise AI-powered internal knowledge assistant for contact centers and support operations. The system includes a .NET backend, Angular frontend, SQL Server database, background document processing, AI provider integrations, document storage, retrieval infrastructure, observability, and secure configuration.

The purpose of this document is to define the deployment and DevOps strategy for:

- Local development.
- Development environment.
- Staging environment.
- Production target.
- Docker usage.
- GitHub Actions.
- Build pipeline.
- Test pipeline.
- Secrets strategy.
- Cloud deployment target.
- Rollback considerations.

This document is intended for human developers, AI coding agents, technical reviewers, and future maintainers.

---

## 2. Deployment Goals

The deployment and DevOps strategy should support the following goals:

1. Make the system easy to run locally.
2. Make build and test validation repeatable.
3. Keep secrets out of source control.
4. Support Docker-based local development.
5. Support GitHub Actions for continuous integration.
6. Prepare the system for Azure deployment.
7. Separate application configuration by environment.
8. Support automated validation before merging changes.
9. Preserve security and organization-scope assumptions across environments.
10. Avoid overengineering deployment before the MVP workflow is stable.

---

# 3. Local Environment

## 3.1 Purpose

The local environment is used by developers to build, run, test, and debug KnowledgeOps-AI on their development machine.

The local environment should allow a developer to run the main MVP workflow without requiring a full cloud deployment.

## 3.2 Local Components

The local environment should support:

- ASP.NET Core Web API.
- Angular frontend.
- SQL Server local instance or SQL Server container.
- Background worker.
- Local document storage.
- Fake or sandbox AI providers where practical.
- Optional real Azure OpenAI or OpenAI provider configuration.
- Local structured logs.
- Local development secrets.

## 3.3 Recommended Local Stack

```text
Frontend:
  Angular development server

Backend:
  .NET 10 ASP.NET Core Web API

Worker:
  .NET background worker service

Database:
  SQL Server container or local SQL Server Developer Edition

Document Storage:
  Local filesystem storage for uploaded files

AI Provider:
  Fake provider by default for tests
  Azure OpenAI or OpenAI API when manually configured

Observability:
  Console logs and local structured logging
```

## 3.4 Local Environment Configuration

Recommended local configuration files:

```text
src/KnowledgeOps.Api/appsettings.json
src/KnowledgeOps.Api/appsettings.Development.json
src/KnowledgeOps.Worker/appsettings.json
src/KnowledgeOps.Worker/appsettings.Development.json
frontend/src/environments/environment.ts
frontend/src/environments/environment.development.ts
```

Secrets must not be committed to these files.

For local secrets, use one of:

```text
dotnet user-secrets
environment variables
local .env file excluded by .gitignore
```

## 3.5 Local Startup Options

The project should support at least one reliable local startup path.

Recommended options:

```text
Option A:
  Run SQL Server through Docker Compose.
  Run API locally through dotnet run.
  Run Worker locally through dotnet run.
  Run Frontend locally through Angular CLI.

Option B:
  Run API, Worker, SQL Server, and supporting services through Docker Compose.
  Run Frontend locally or through Docker.

Option C:
  Run the full local stack through Docker Compose.
```

For MVP, Option A or B is acceptable.

## 3.6 Local Developer Workflow

Recommended local workflow:

```text
1. Clone repository.
2. Configure local secrets.
3. Start SQL Server.
4. Apply EF Core migrations.
5. Start API.
6. Start Worker.
7. Start frontend.
8. Log in with seeded demo user.
9. Upload a sample document.
10. Confirm processing status.
11. Ask a chat question.
12. Review citations and feedback.
13. Review dashboard metrics.
```

## 3.7 Local Seed Data

Local development should include seed data for:

- Organization A.
- Organization B.
- Agent A.
- KnowledgeAdmin A.
- Manager A.
- Admin A.
- Agent B for cross-organization tests.

Seed data must not include real sensitive documents or real credentials.

---

# 4. Development Environment

## 4.1 Purpose

The development environment is used to validate integrated work after local development.

This environment may be local-only during early MVP development or hosted later if needed.

## 4.2 Development Environment Characteristics

The development environment should:

- Use development configuration.
- Use non-production data.
- Support test or demo users.
- Allow document upload testing.
- Allow fake or sandbox AI providers.
- Allow integration testing.
- Support logs and diagnostics.
- Be disposable when possible.

## 4.3 Development Environment Data

Development data may include:

- Sample support policies.
- Sample escalation procedures.
- Sample training documents.
- Synthetic chat history.
- Synthetic feedback.
- Synthetic dashboard metrics.

Development data must not include:

- Real customer data.
- Real employee sensitive data.
- Real client confidential documents.
- Production API keys.

## 4.4 Development Environment AI Providers

Preferred development strategy:

```text
Automated tests:
  Fake AI and embedding providers.

Manual development:
  Fake provider by default.
  Optional Azure OpenAI or OpenAI API when configured.

Demo validation:
  Real provider may be used if secrets and cost limits are configured safely.
```

---

# 5. Staging Environment

## 5.1 Purpose

The staging environment is used to validate deployment readiness before production-like usage.

For a portfolio MVP, staging may initially be optional. However, the architecture should support it.

## 5.2 Staging Environment Characteristics

Staging should resemble the production target as closely as practical.

Staging should include:

- Deployed backend API.
- Deployed frontend.
- Deployed background worker.
- SQL database.
- Document storage.
- AI provider configuration.
- Retrieval provider configuration.
- Secrets management.
- Logging and monitoring.
- Seeded non-production users.
- Sample documents.

## 5.3 Staging Validation Goals

Staging should validate:

- Application startup.
- Database migration compatibility.
- Authentication.
- Role-based authorization.
- Organization-scope enforcement.
- Document upload.
- Document processing.
- Embedding generation.
- Retrieval.
- RAG chat.
- Citations.
- Feedback.
- Dashboard metrics.
- Health endpoints.
- Logs and telemetry.
- Secret resolution.

## 5.4 Staging Data Rules

Staging must use synthetic or approved demo data only.

Staging must not use real customer, employee, or client-sensitive data unless a formal data handling policy exists.

---

# 6. Production Target

## 6.1 Purpose

The production target describes the intended future deployment model if KnowledgeOps-AI were deployed as a real enterprise application.

For the portfolio MVP, full production deployment may be deferred, but the architecture should remain production-aware.

## 6.2 Production Target Characteristics

The production target should support:

- Secure hosting.
- Environment-based configuration.
- Secrets management.
- Managed database.
- Managed document storage.
- AI provider integration.
- Retrieval infrastructure.
- Monitoring and alerting.
- CI/CD deployment.
- Rollback capability.
- Audit-sensitive logging.
- Organization-aware data protection.

## 6.3 Production Target Deployment Shape

Recommended Azure target:

```text
Frontend:
  Azure Static Web Apps or Azure App Service

Backend API:
  Azure App Service or Azure Container Apps

Background Worker:
  Azure WebJob, Azure Container App, Azure App Service worker, or Azure Functions depending on design

Database:
  Azure SQL Database

Document Storage:
  Azure Blob Storage

AI Provider:
  Azure OpenAI

Vector Retrieval:
  Azure AI Search or SQL vector-compatible storage

Secrets:
  Azure Key Vault

Observability:
  Application Insights
```

## 6.4 Production Readiness Caveat

The MVP does not need to implement every production-grade feature immediately.

The MVP should be production-aware, not production-overbuilt.

Production hardening may be deferred to later phases.

---

# 7. Docker Strategy

## 7.1 Purpose

Docker should make local development and environment validation more reproducible.

Docker should help run:

- SQL Server.
- API.
- Worker.
- Frontend, optionally.
- Supporting services, when applicable.

## 7.2 Recommended Docker Files

Recommended files:

```text
Dockerfile.api
Dockerfile.worker
Dockerfile.frontend
docker-compose.yml
docker-compose.override.yml
```

Alternative structure:

```text
src/KnowledgeOps.Api/Dockerfile
src/KnowledgeOps.Worker/Dockerfile
frontend/Dockerfile
docker-compose.yml
```

Either structure is acceptable if documented clearly.

## 7.3 Docker Compose Services

Recommended local Docker Compose services:

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${SQL_PASSWORD}"
    ports:
      - "1433:1433"

  api:
    build:
      context: .
      dockerfile: src/KnowledgeOps.Api/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Development
    depends_on:
      - sqlserver

  worker:
    build:
      context: .
      dockerfile: src/KnowledgeOps.Worker/Dockerfile
    environment:
      DOTNET_ENVIRONMENT: Development
    depends_on:
      - sqlserver

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "4200:80"
    depends_on:
      - api
```

This is a conceptual example. Exact ports, names, paths, and environment variables should be defined during implementation.

## 7.4 Docker Development Rules

Docker configuration must:

- Avoid hardcoded secrets.
- Use environment variables.
- Support local development.
- Support repeatable startup.
- Avoid storing uploaded files inside disposable containers unless using mounted volumes.
- Use volumes for SQL Server data if persistence is needed locally.
- Clearly document reset behavior.

## 7.5 Docker Volumes

Recommended local volumes:

```text
sqlserver_data
knowledgeops_uploaded_files
```

These volumes help preserve database and uploaded document files across container restarts.

## 7.6 Docker and AI Providers

Docker should not include real AI provider secrets.

AI provider configuration should come from:

- Environment variables.
- User secrets.
- Secret manager.
- CI/CD secrets.

---

# 8. GitHub Actions

## 8.1 Purpose

GitHub Actions should validate code before changes are merged.

The CI pipeline should provide confidence that the project builds, tests pass, and core quality gates remain intact.

## 8.2 Recommended Workflows

Recommended workflow files:

```text
.github/workflows/backend-ci.yml
.github/workflows/frontend-ci.yml
.github/workflows/docs-ci.yml
.github/workflows/docker-ci.yml
```

For MVP, these can be consolidated into one workflow:

```text
.github/workflows/ci.yml
```

## 8.3 Pull Request Validation

Pull requests should validate:

- Backend restore.
- Backend build.
- Backend unit tests.
- Backend integration tests where practical.
- Frontend install.
- Frontend build.
- Frontend tests.
- Formatting or linting where configured.
- Docker build where practical.
- Documentation checks where practical.

## 8.4 Branch Strategy

Recommended branch strategy:

```text
main
feature/<issue-number>-short-description
fix/<issue-number>-short-description
docs/<issue-number>-short-description
```

Examples:

```text
feature/23-document-upload
feature/31-rag-chat
fix/42-dashboard-scope-filter
docs/18-deployment-devops
```

## 8.5 Pull Request Expectations

Pull requests should include:

- Summary.
- Added / changed behavior.
- Validation performed.
- Scope confirmation.
- Related issue.
- Checklist.
- Documentation updates when needed.

---

# 9. Build Pipeline

## 9.1 Backend Build Pipeline

Backend build steps:

```text
1. Checkout repository.
2. Set up .NET SDK.
3. Restore dependencies.
4. Build solution.
5. Run unit tests.
6. Run integration tests where practical.
7. Publish API artifact if needed.
8. Publish Worker artifact if needed.
```

Example conceptual commands:

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet publish src/KnowledgeOps.Api/KnowledgeOps.Api.csproj --configuration Release --output ./artifacts/api
dotnet publish src/KnowledgeOps.Worker/KnowledgeOps.Worker.csproj --configuration Release --output ./artifacts/worker
```

## 9.2 Frontend Build Pipeline

Frontend build steps:

```text
1. Checkout repository.
2. Set up Node.js.
3. Install dependencies.
4. Run linting if configured.
5. Run frontend tests.
6. Build frontend.
7. Publish frontend artifact if needed.
```

Example conceptual commands:

```bash
cd frontend
npm ci
npm run lint
npm test -- --watch=false
npm run build
```

Exact commands may change depending on selected Angular tooling.

## 9.3 Docker Build Pipeline

Docker build steps:

```text
1. Build API image.
2. Build Worker image.
3. Build Frontend image.
4. Optionally run container smoke test.
```

Example conceptual commands:

```bash
docker build -f src/KnowledgeOps.Api/Dockerfile -t knowledgeops-api:ci .
docker build -f src/KnowledgeOps.Worker/Dockerfile -t knowledgeops-worker:ci .
docker build -f frontend/Dockerfile -t knowledgeops-frontend:ci ./frontend
```

## 9.4 Build Artifacts

Possible build artifacts:

```text
API publish output
Worker publish output
Frontend dist output
Docker images
Test result files
Coverage reports
```

For MVP, published artifacts are optional unless deployment automation is implemented.

---

# 10. Test Pipeline

## 10.1 Test Pipeline Purpose

The test pipeline validates that code changes do not break documented behavior.

Testing must remain connected to:

- Use cases.
- Business rules.
- API contracts.
- Security rules.
- Acceptance criteria.

## 10.2 Backend Test Pipeline

Recommended backend test stages:

```text
Unit tests:
  Fast, deterministic, no external dependencies.

Integration tests:
  Use test database or Testcontainers where practical.

API tests:
  Validate HTTP contracts and authorization.

Security tests:
  Validate role and organization-scope enforcement.
```

## 10.3 Frontend Test Pipeline

Recommended frontend test stages:

```text
Unit/component tests:
  Components, services, forms, guards.

Integration-style frontend tests:
  API client behavior using mocks.

E2E smoke tests:
  Critical flows using seeded test data.
```

## 10.4 AI Provider Testing Strategy

Automated tests should use:

```text
FakeEmbeddingProvider
FakeAiAnswerGenerator
FakeCostEstimator
FakeRetrievalProvider when needed
```

Automated tests should not require:

```text
Live Azure OpenAI calls
Live OpenAI API calls
Real production secrets
External network access
```

## 10.5 E2E Test Pipeline

E2E tests may be run:

- On demand.
- Nightly.
- Before release.
- On pull requests only if stable and fast enough.

Recommended E2E smoke flows:

```text
Login
Upload document
Process document
Ask question
Review citations
Submit feedback
View dashboard
Reject unauthorized access
```

## 10.6 Test Data Strategy

Test data should use:

- Synthetic organizations.
- Synthetic users.
- Synthetic documents.
- Deterministic fake AI responses.
- Deterministic fake embeddings.
- Controlled timestamps where practical.

Test data must not use real customer or internal confidential data.

---

# 11. Secrets Strategy

## 11.1 Secrets Principles

Secrets must never be committed to source control.

Secrets include:

- Database passwords.
- JWT signing keys.
- Azure OpenAI keys.
- OpenAI API keys.
- Azure Blob Storage connection strings.
- Azure Key Vault credentials.
- Application Insights connection strings.
- Any provider token or credential.

## 11.2 Local Secrets

Local development may use:

```text
dotnet user-secrets
environment variables
.env files excluded by .gitignore
```

Recommended `.gitignore` entries:

```text
.env
.env.*
*.local.json
appsettings.Local.json
```

## 11.3 GitHub Actions Secrets

GitHub Actions should use repository or environment secrets.

Examples:

```text
AZURE_OPENAI_ENDPOINT
AZURE_OPENAI_API_KEY
OPENAI_API_KEY
SQL_CONNECTION_STRING
JWT_SIGNING_KEY
AZURE_STORAGE_CONNECTION_STRING
APPLICATIONINSIGHTS_CONNECTION_STRING
```

For CI tests, prefer fake providers so most secrets are not needed.

## 11.4 Azure Secrets

Azure deployment should use:

```text
Azure Key Vault
Managed Identity where possible
App Service configuration
Container App secrets
GitHub Actions environment secrets
```

## 11.5 Secret Rotation

Future production-ready deployment should support:

- Rotating API keys.
- Rotating JWT signing secrets.
- Rotating database credentials.
- Revoking compromised provider keys.
- Updating deployed configuration without source changes.

## 11.6 Secret Logging Restrictions

The system must not log:

- API keys.
- Connection strings.
- JWT signing keys.
- Raw authentication tokens.
- Provider credentials.
- Secret configuration values.

---

# 12. Cloud Deployment Target

## 12.1 Azure Target

KnowledgeOps-AI is intended to be Azure-ready.

Recommended Azure target:

| Component | Azure Service |
|---|---|
| Frontend | Azure Static Web Apps or Azure App Service |
| Backend API | Azure App Service or Azure Container Apps |
| Background Worker | Azure WebJob, Azure Container Apps, Azure Functions, or Worker App Service |
| Database | Azure SQL Database |
| Document Storage | Azure Blob Storage |
| AI Provider | Azure OpenAI |
| Vector Retrieval | Azure AI Search or SQL vector-compatible strategy |
| Secrets | Azure Key Vault |
| Observability | Application Insights |
| CI/CD | GitHub Actions |

## 12.2 MVP Cloud Deployment Scope

For MVP, cloud deployment may be limited to:

- API deployable artifact.
- Frontend deployable artifact.
- Database migration strategy.
- Environment-based configuration.
- Azure-ready provider abstractions.
- Documented deployment path.

Full production-grade Azure deployment may be deferred.

## 12.3 Recommended Azure Deployment Flow

Conceptual flow:

```text
1. Merge approved PR into main.
2. GitHub Actions runs build and tests.
3. Build deployable artifacts or container images.
4. Push images or artifacts to deployment target.
5. Apply database migrations safely.
6. Deploy API.
7. Deploy Worker.
8. Deploy Frontend.
9. Run smoke tests.
10. Monitor health and logs.
```

## 12.4 Database Migration Strategy

Database migrations should be handled carefully.

Options:

```text
Option A:
  Apply EF Core migrations manually during MVP.

Option B:
  Apply EF Core migrations through controlled deployment step.

Option C:
  Generate SQL migration scripts and review before applying.
```

Recommended for MVP:

```text
Generate and review migrations.
Apply migrations intentionally.
Avoid automatic destructive migrations in production-like environments.
```

## 12.5 Infrastructure as Code

Infrastructure as Code may be deferred.

Future options:

```text
Bicep
Terraform
Azure Developer CLI
ARM templates
GitHub Actions deployment scripts
```

For MVP, clear deployment documentation is acceptable before full IaC.

---

# 13. Rollback Considerations

## 13.1 Purpose

Rollback planning defines how to recover if a deployment introduces a failure.

Even in a portfolio MVP, rollback thinking demonstrates operational maturity.

## 13.2 Rollback Targets

Rollback may apply to:

- Backend API.
- Background Worker.
- Frontend.
- Database migration.
- Configuration.
- AI provider configuration.
- Retrieval provider configuration.

## 13.3 Application Rollback

Application rollback should support:

```text
Redeploy previous API artifact or image.
Redeploy previous Worker artifact or image.
Redeploy previous Frontend artifact.
Restore previous configuration where applicable.
```

## 13.4 Database Rollback

Database rollback is more sensitive than application rollback.

Recommended approach:

- Avoid destructive migrations during MVP.
- Review generated migration SQL.
- Back up database before production-like migrations.
- Prefer additive schema changes where possible.
- Document manual rollback steps for each migration.
- Do not assume every migration can be automatically rolled back safely.

## 13.5 Configuration Rollback

Configuration rollback should include:

- Previous environment variable values.
- Previous AI provider settings.
- Previous retrieval settings.
- Previous feature flags if used.
- Previous connection strings if rotated.

## 13.6 Frontend Rollback

Frontend rollback is usually simpler.

Rollback approach:

```text
Redeploy previous frontend build artifact.
Confirm API contract compatibility.
Run smoke test.
```

## 13.7 Worker Rollback

Worker rollback requires caution because workers may process documents.

Rollback approach:

```text
Stop worker.
Deploy previous worker version.
Verify document processing status consistency.
Restart worker.
Inspect failed or partially processed documents.
```

## 13.8 AI and Retrieval Rollback

If an AI prompt, embedding model, or retrieval configuration causes poor answer quality:

Rollback approach:

```text
Revert prompt template version.
Revert retrieval configuration.
Revert embedding model only if compatible.
Reprocess embeddings if required.
Validate with known questions.
```

Embedding model changes may require re-indexing documents, so they should be treated as migration-like changes.

## 13.9 Rollback Smoke Tests

After rollback, validate:

- API health.
- Login.
- Document list.
- Chat question.
- Citation display.
- Feedback submission.
- Dashboard overview.
- Worker status.
- Logs for critical errors.

---

# 14. Environment Configuration Matrix

| Setting | Local | Development | Staging | Production Target |
|---|---|---|---|---|
| ASPNETCORE_ENVIRONMENT | Development | Development | Staging | Production |
| Database | SQL container/local | Dev SQL | Staging SQL | Azure SQL |
| Document storage | Local filesystem | Local/blob dev | Azure Blob staging | Azure Blob production |
| AI provider | Fake or sandbox | Fake or sandbox | Azure OpenAI staging | Azure OpenAI production |
| Vector retrieval | In-memory/dev SQL/vector | Dev vector strategy | Azure AI Search or equivalent | Azure AI Search or equivalent |
| Secrets | User secrets/.env | GitHub/env secrets | Key Vault/env secrets | Key Vault/managed identity |
| Observability | Console logs | Structured logs | App Insights staging | App Insights production |
| Worker | Local process/container | Dev process/container | Deployed worker | Deployed worker |
| Frontend | Angular dev server | Dev build | Staging build | Production build |

---

# 15. Deployment and DevOps Traceability

## 15.1 DevOps to Architecture Traceability

| DevOps Area | Related Architecture Concern |
|---|---|
| Docker | Deployment view, local environment, reproducibility. |
| GitHub Actions | CI validation, build/test pipeline. |
| Secrets strategy | Security, provider isolation, cloud readiness. |
| Azure target | Deployment view, production target. |
| Worker deployment | Asynchronous document processing. |
| Database migrations | SQL Server persistence and data integrity. |
| Observability | Logging, metrics, operational diagnostics. |
| Rollback | Operational resilience. |

## 15.2 DevOps to Security Traceability

| DevOps Area | Security Concern |
|---|---|
| Environment variables | Avoid hardcoded secrets. |
| GitHub Actions secrets | Protect CI/CD credentials. |
| Key Vault | Secure production secret storage. |
| Docker Compose | Prevent secrets from being committed. |
| Logs | Avoid leaking sensitive content. |
| Deployment permissions | Limit who can deploy or modify secrets. |
| Rollback | Recover from bad configuration or unsafe releases. |

## 15.3 DevOps to Testing Traceability

| Pipeline Stage | Related Testing Area |
|---|---|
| Backend build | Compile-time validation. |
| Backend unit tests | Business rules and application logic. |
| Backend integration tests | Persistence, authorization, workflows. |
| API tests | HTTP contracts and security. |
| Frontend build | UI compile validation. |
| Frontend tests | Components, guards, forms, API clients. |
| E2E tests | Critical user journeys. |
| Docker build | Container deployability. |

---

# 16. DevOps Guidance for AI Agents

AI coding agents must follow this document when modifying deployment, Docker, CI/CD, configuration, secrets, or environment behavior.

## 16.1 AI Agents Must

- Keep secrets out of source control.
- Use environment-based configuration.
- Preserve local developer usability.
- Preserve Docker reproducibility.
- Update GitHub Actions when build/test commands change.
- Avoid requiring live AI provider calls in normal CI.
- Keep deployment scripts aligned with the documented architecture.
- Update documentation when deployment behavior changes.
- Treat database migrations carefully.
- Include rollback notes for risky deployment changes.
- Preserve API, Worker, Frontend, and database separation.

## 16.2 AI Agents Must Not

- Commit real API keys, passwords, or connection strings.
- Hardcode provider credentials.
- Make CI depend on real Azure OpenAI or OpenAI calls by default.
- Add production-only assumptions to local development.
- Apply destructive database migrations silently.
- Remove test stages to make CI pass without explanation.
- Store uploaded files only inside disposable containers without documenting data loss behavior.
- Expose secrets through logs.
- Add cloud resources outside the approved architecture without an ADR.

---

# 17. Summary

This document defines how KnowledgeOps-AI moves from local development to deployable software.

The DevOps strategy supports:

- Local development.
- Development validation.
- Optional staging.
- Azure-ready production target.
- Docker reproducibility.
- GitHub Actions CI.
- Build and test pipelines.
- Secure secrets handling.
- Cloud deployment planning.
- Rollback awareness.

The project does not need full production-grade infrastructure from the first MVP, but it must be designed so that deployment, testing, configuration, and secrets are handled professionally.

The goal is a system that can be developed locally, validated automatically, configured securely, deployed predictably, and evolved toward a realistic Azure-hosted enterprise AI platform.
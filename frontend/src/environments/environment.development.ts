// Development environment.
// API base URL points to the KnowledgeOps.Api local dev server (port established in launchSettings.json).
// This value may change when Docker Compose local environment is configured in Sprint 3.
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5194/api/v1'
};

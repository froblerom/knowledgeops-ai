namespace KnowledgeOps.Api.Controllers.Models;

public sealed record BasicHealthResponse(string Status, DateTimeOffset Timestamp);

public sealed record DetailedHealthResponse(
    string Status,
    HealthDependencyResponse Dependencies,
    DateTimeOffset Timestamp);

public sealed record HealthDependencyResponse(string Application, string Database);

namespace KnowledgeOps.Application.Chat.Prompting;

public sealed record ContextSufficiencyResult(bool IsSufficient, string? FailureCode);

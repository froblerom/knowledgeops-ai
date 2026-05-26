namespace KnowledgeOps.Application.Observability;

public static class CorrelationIdPolicy
{
    public const int MaximumLength = 100;

    public static string AcceptOrCreate(string? incomingCorrelationId)
    {
        return IsAccepted(incomingCorrelationId)
            ? incomingCorrelationId!
            : Guid.NewGuid().ToString("N");
    }

    public static bool IsAccepted(string? correlationId)
    {
        if (string.IsNullOrEmpty(correlationId) || correlationId.Length > MaximumLength)
            return false;

        return correlationId.All(character =>
            char.IsAsciiLetterOrDigit(character)
            || character == '-'
            || character == '_');
    }
}

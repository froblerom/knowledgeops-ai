namespace KnowledgeOps.Application.Errors;

public sealed class ApplicationValidationException : Exception
{
    public ApplicationValidationException(IReadOnlyList<ApplicationValidationItem> details)
        : base("One or more validation errors occurred.")
    {
        Details = details;
    }

    public IReadOnlyList<ApplicationValidationItem> Details { get; }
}

public sealed record ApplicationValidationItem(string Field, string Message);

namespace KnowledgeOps.Application.Errors;

public sealed class ApplicationUnauthenticatedException : Exception
{
}

public sealed class ApplicationForbiddenException : Exception
{
}

public sealed class ApplicationNotFoundException : Exception
{
}

public sealed class ApplicationConflictException : Exception
{
}

public sealed class ApplicationServiceUnavailableException : Exception
{
}

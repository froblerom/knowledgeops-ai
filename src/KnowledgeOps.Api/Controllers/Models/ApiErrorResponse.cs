using KnowledgeOps.Application.Observability;

namespace KnowledgeOps.Api.Controllers.Models;

public sealed class ApiErrorResponse
{
    public ApiErrorBody Error { get; init; } = new();
}

public sealed class ApiErrorBody
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<ApiValidationItem> Details { get; init; } = [];
    public string CorrelationId { get; init; } = string.Empty;
}

public sealed record ApiValidationItem(string Field, string Message);

internal static class ApiErrorResponses
{
    public const string ValidationCode = "validation_error";
    public const string UnauthenticatedCode = "unauthenticated";
    public const string ForbiddenCode = "forbidden";
    public const string NotFoundCode = "not_found";
    public const string ConflictCode = "conflict";
    public const string InternalErrorCode = "internal_error";
    public const string ServiceUnavailableCode = "service_unavailable";

    public static ApiErrorResponse Create(
        string code,
        string message,
        string correlationId,
        IReadOnlyList<ApiValidationItem>? details = null) =>
        new()
        {
            Error = new ApiErrorBody
            {
                Code = code,
                Message = message,
                Details = details ?? [],
                CorrelationId = CorrelationIdPolicy.AcceptOrCreate(correlationId)
            }
        };

    public static ApiErrorResponse InvalidCredentials(string correlationId) =>
        Create(UnauthenticatedCode, "Invalid credentials.", correlationId);

    public static ApiErrorResponse Unauthenticated(string correlationId) =>
        Create(UnauthenticatedCode, "Authentication is required or invalid.", correlationId);

    public static ApiErrorResponse Forbidden(string correlationId) =>
        Create(ForbiddenCode, "You are not authorized to perform this action.", correlationId);

    public static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        ApiErrorResponse response)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }
}

using KnowledgeOps.Api.Controllers.Models;
using KnowledgeOps.Application.Errors;
using KnowledgeOps.Application.Observability;

namespace KnowledgeOps.Api.Middleware;

public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlation)
    {
        try
        {
            await next(context);
        }
        catch (ApplicationValidationException exception)
        {
            var details = exception.Details
                .Select(detail => new ApiValidationItem(detail.Field, detail.Message))
                .ToArray();

            await ApiErrorResponses.WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                ApiErrorResponses.Create(
                    ApiErrorResponses.ValidationCode,
                    "One or more validation errors occurred.",
                    correlation.CorrelationId,
                    details));
        }
        catch (ApplicationUnauthenticatedException)
        {
            await ApiErrorResponses.WriteAsync(
                context,
                StatusCodes.Status401Unauthorized,
                ApiErrorResponses.Unauthenticated(correlation.CorrelationId));
        }
        catch (ApplicationForbiddenException)
        {
            await ApiErrorResponses.WriteAsync(
                context,
                StatusCodes.Status403Forbidden,
                ApiErrorResponses.Forbidden(correlation.CorrelationId));
        }
        catch (ApplicationNotFoundException)
        {
            await WriteErrorAsync(
                context,
                correlation.CorrelationId,
                StatusCodes.Status404NotFound,
                ApiErrorResponses.NotFoundCode,
                "The requested resource was not found.");
        }
        catch (ApplicationConflictException)
        {
            await WriteErrorAsync(
                context,
                correlation.CorrelationId,
                StatusCodes.Status409Conflict,
                ApiErrorResponses.ConflictCode,
                "The request conflicts with the current state.");
        }
        catch (ApplicationServiceUnavailableException)
        {
            await WriteErrorAsync(
                context,
                correlation.CorrelationId,
                StatusCodes.Status503ServiceUnavailable,
                ApiErrorResponses.ServiceUnavailableCode,
                "The service is temporarily unavailable. Please try again later.");
        }
        catch (Exception)
        {
            logger.LogError(
                "Unhandled API request failure. CorrelationId={CorrelationId}",
                correlation.CorrelationId);

            await WriteErrorAsync(
                context,
                correlation.CorrelationId,
                StatusCodes.Status500InternalServerError,
                ApiErrorResponses.InternalErrorCode,
                "An unexpected error occurred.");
        }
    }

    private static Task WriteErrorAsync(
        HttpContext context,
        string correlationId,
        int statusCode,
        string code,
        string message) =>
        ApiErrorResponses.WriteAsync(
            context,
            statusCode,
            ApiErrorResponses.Create(code, message, correlationId));
}

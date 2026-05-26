using KnowledgeOps.Api.Observability;
using KnowledgeOps.Application.Observability;

namespace KnowledgeOps.Api.Middleware;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var requestedId = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = CorrelationIdPolicy.AcceptOrCreate(requestedId);

        context.Items[HttpCorrelationContext.HttpContextItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await next(context);
        }
    }
}

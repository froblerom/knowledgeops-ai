using KnowledgeOps.Application.Observability;

namespace KnowledgeOps.Api.Observability;

public sealed class HttpCorrelationContext(IHttpContextAccessor httpContextAccessor) : ICorrelationContext
{
    internal const string HttpContextItemKey = "KnowledgeOps.CorrelationId";

    public string CorrelationId =>
        httpContextAccessor.HttpContext?.Items[HttpContextItemKey] as string
        ?? CorrelationIdPolicy.AcceptOrCreate(null);
}

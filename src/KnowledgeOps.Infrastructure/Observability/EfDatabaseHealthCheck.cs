using KnowledgeOps.Application.Observability;
using KnowledgeOps.Infrastructure.Persistence;

namespace KnowledgeOps.Infrastructure.Observability;

public sealed class EfDatabaseHealthCheck(KnowledgeOpsDbContext dbContext) : IDatabaseHealthCheck
{
    public async Task<DatabaseHealthResult> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(ct)
                ? DatabaseHealthResult.Healthy
                : DatabaseHealthResult.Unavailable;
        }
        catch
        {
            return DatabaseHealthResult.Unavailable;
        }
    }
}

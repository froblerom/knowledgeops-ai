using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KnowledgeOps.Infrastructure.Persistence;

public sealed class KnowledgeOpsDbContextFactory : IDesignTimeDbContextFactory<KnowledgeOpsDbContext>
{
    public KnowledgeOpsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Design-time EF Core operations require environment variable " +
                "'ConnectionStrings__DefaultConnection' to be set.");
        }

        var options = new DbContextOptionsBuilder<KnowledgeOpsDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new KnowledgeOpsDbContext(options);
    }
}

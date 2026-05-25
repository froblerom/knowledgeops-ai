using KnowledgeOps.Domain.Audit;
using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Persistence;

public sealed class KnowledgeOpsDbContext(DbContextOptions<KnowledgeOpsDbContext> options)
    : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KnowledgeOpsDbContext).Assembly);
    }
}

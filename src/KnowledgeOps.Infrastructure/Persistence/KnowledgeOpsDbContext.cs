using KnowledgeOps.Domain.Audit;
using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using KnowledgeOps.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Persistence;

public sealed class KnowledgeOpsDbContext(DbContextOptions<KnowledgeOpsDbContext> options)
    : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<ChunkEmbedding> ChunkEmbeddings => Set<ChunkEmbedding>();

    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();

    public DbSet<ChatInteraction> ChatInteractions => Set<ChatInteraction>();

    public DbSet<Citation> Citations => Set<Citation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KnowledgeOpsDbContext).Assembly);
        KnowledgeOpsSeedData.ApplySeedData(modelBuilder);
    }
}

using KnowledgeOps.Domain.Audit;
using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeOps.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log_entries", table =>
        {
            table.HasCheckConstraint(
                "CK_audit_log_entries_severity",
                "[severity] IN (N'Info', N'Warning', N'Error', N'Critical')");
        });

        builder.HasKey(auditEntry => auditEntry.Id)
            .HasName("PK_audit_log_entries");

        builder.Property(auditEntry => auditEntry.Id)
            .HasColumnName("audit_log_entry_id");

        builder.Property(auditEntry => auditEntry.OrganizationId)
            .HasColumnName("organization_id");

        builder.Property(auditEntry => auditEntry.UserId)
            .HasColumnName("user_id");

        builder.Property(auditEntry => auditEntry.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(auditEntry => auditEntry.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(150);

        builder.Property(auditEntry => auditEntry.EntityId)
            .HasColumnName("entity_id");

        builder.Property(auditEntry => auditEntry.Message)
            .HasColumnName("message")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(auditEntry => auditEntry.Severity)
            .HasColumnName("severity")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(auditEntry => auditEntry.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(100);

        builder.Property(auditEntry => auditEntry.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(auditEntry => auditEntry.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_audit_log_entries_organizations_organization_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(auditEntry => auditEntry.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_audit_log_entries_users_user_id");

        builder.HasIndex(auditEntry => auditEntry.OrganizationId)
            .HasDatabaseName("IX_audit_log_entries_organization_id");

        builder.HasIndex(auditEntry => auditEntry.UserId)
            .HasDatabaseName("IX_audit_log_entries_user_id");

        builder.HasIndex(auditEntry => auditEntry.EventType)
            .HasDatabaseName("IX_audit_log_entries_event_type");

        builder.HasIndex(auditEntry => auditEntry.CreatedAt)
            .HasDatabaseName("IX_audit_log_entries_created_at");

        builder.HasIndex(auditEntry => auditEntry.CorrelationId)
            .HasDatabaseName("IX_audit_log_entries_correlation_id");
    }
}

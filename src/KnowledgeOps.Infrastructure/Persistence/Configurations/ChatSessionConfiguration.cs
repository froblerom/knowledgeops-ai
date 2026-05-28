using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeOps.Infrastructure.Persistence.Configurations;

internal sealed class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("chat_sessions");

        builder.HasKey(s => s.Id)
            .HasName("PK_chat_sessions");

        builder.Property(s => s.Id)
            .HasColumnName("chat_session_id");

        builder.Property(s => s.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(s => s.Title)
            .HasColumnName("title")
            .HasMaxLength(500);

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.Property(s => s.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Optional);

        builder.Property(s => s.LastInteractionAt)
            .HasColumnName("last_interaction_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Optional);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_chat_sessions_organizations_organization_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_chat_sessions_users_user_id");

        builder.HasIndex(s => s.OrganizationId)
            .HasDatabaseName("IX_chat_sessions_organization_id");

        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("IX_chat_sessions_user_id");

        builder.HasIndex(s => new { s.OrganizationId, s.CreatedAt })
            .HasDatabaseName("IX_chat_sessions_organization_id_created_at");
    }
}

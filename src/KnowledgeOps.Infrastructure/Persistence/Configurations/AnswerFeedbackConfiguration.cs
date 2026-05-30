using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeOps.Infrastructure.Persistence.Configurations;

internal sealed class AnswerFeedbackConfiguration : IEntityTypeConfiguration<AnswerFeedback>
{
    public void Configure(EntityTypeBuilder<AnswerFeedback> builder)
    {
        builder.ToTable("answer_feedback", table =>
        {
            table.HasCheckConstraint(
                "CK_answer_feedback_rating",
                "[rating] IN (N'Useful', N'NotUseful')");
        });

        builder.HasKey(f => f.Id)
            .HasName("PK_answer_feedback");

        builder.Property(f => f.Id)
            .HasColumnName("answer_feedback_id");

        builder.Property(f => f.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(f => f.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(f => f.ChatInteractionId)
            .HasColumnName("chat_interaction_id")
            .IsRequired();

        builder.Property(f => f.Rating)
            .HasColumnName("rating")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.Property(f => f.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(f => f.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_answer_feedback_organizations_organization_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_answer_feedback_users_user_id");

        builder.HasOne<ChatInteraction>()
            .WithMany()
            .HasForeignKey(f => f.ChatInteractionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_answer_feedback_chat_interactions_chat_interaction_id");

        builder.HasIndex(f => f.ChatInteractionId)
            .HasDatabaseName("IX_answer_feedback_chat_interaction_id");

        builder.HasIndex(f => f.OrganizationId)
            .HasDatabaseName("IX_answer_feedback_organization_id");

        builder.HasIndex(f => f.UserId)
            .HasDatabaseName("IX_answer_feedback_user_id");

        builder.HasIndex(f => new { f.ChatInteractionId, f.UserId })
            .IsUnique()
            .HasDatabaseName("UX_answer_feedback_chat_interaction_id_user_id");

        builder.HasIndex(f => new { f.OrganizationId, f.Rating })
            .HasDatabaseName("IX_answer_feedback_organization_id_rating");
    }
}

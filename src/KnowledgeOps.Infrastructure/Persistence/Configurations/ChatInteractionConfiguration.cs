using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeOps.Infrastructure.Persistence.Configurations;

internal sealed class ChatInteractionConfiguration : IEntityTypeConfiguration<ChatInteraction>
{
    public void Configure(EntityTypeBuilder<ChatInteraction> builder)
    {
        builder.ToTable("chat_interactions");

        builder.HasKey(i => i.Id)
            .HasName("PK_chat_interactions");

        builder.Property(i => i.Id)
            .HasColumnName("chat_interaction_id");

        builder.Property(i => i.ChatSessionId)
            .HasColumnName("chat_session_id")
            .IsRequired();

        builder.Property(i => i.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(i => i.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(i => i.QuestionText)
            .HasColumnName("question_text")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(i => i.QuestionTextHash)
            .HasColumnName("question_text_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(i => i.AnswerText)
            .HasColumnName("answer_text")
            .HasColumnType("nvarchar(max)");

        builder.Property(i => i.AnswerState)
            .HasColumnName("answer_state")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(i => i.RetrievalQueryId)
            .HasColumnName("retrieval_query_id");

        builder.Property(i => i.RetrievalCandidateCount)
            .HasColumnName("retrieval_candidate_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(i => i.RetrievalLatencyMs)
            .HasColumnName("retrieval_latency_ms")
            .HasColumnType("bigint");

        builder.Property(i => i.GenerationLatencyMs)
            .HasColumnName("generation_latency_ms")
            .HasColumnType("bigint");

        builder.Property(i => i.TotalLatencyMs)
            .HasColumnName("total_latency_ms")
            .HasColumnType("bigint");

        builder.Property(i => i.AiProvider)
            .HasColumnName("ai_provider")
            .HasMaxLength(100);

        builder.Property(i => i.AiModel)
            .HasColumnName("ai_model")
            .HasMaxLength(100);

        builder.Property(i => i.TokenUsageInput)
            .HasColumnName("token_usage_input");

        builder.Property(i => i.TokenUsageOutput)
            .HasColumnName("token_usage_output");

        builder.Property(i => i.EstimatedCost)
            .HasColumnName("estimated_cost")
            .HasColumnType("decimal(18,6)");

        builder.Property(i => i.ProviderFailureCode)
            .HasColumnName("provider_failure_code")
            .HasMaxLength(100);

        builder.Property(i => i.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(100);

        builder.Property(i => i.PromptVersion)
            .HasColumnName("prompt_version")
            .HasMaxLength(50);

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.HasOne<ChatSession>()
            .WithMany()
            .HasForeignKey(i => i.ChatSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_chat_interactions_chat_sessions_chat_session_id");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_chat_interactions_organizations_organization_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_chat_interactions_users_user_id");

        builder.HasIndex(i => i.ChatSessionId)
            .HasDatabaseName("IX_chat_interactions_chat_session_id");

        builder.HasIndex(i => new { i.OrganizationId, i.CreatedAt })
            .HasDatabaseName("IX_chat_interactions_organization_id_created_at");

        builder.HasIndex(i => i.UserId)
            .HasDatabaseName("IX_chat_interactions_user_id");

        builder.HasIndex(i => i.AnswerState)
            .HasDatabaseName("IX_chat_interactions_answer_state");
    }
}

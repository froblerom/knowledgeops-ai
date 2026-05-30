using KnowledgeOps.Domain.Chat;
using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeOps.Infrastructure.Persistence.Configurations;

internal sealed class CitationConfiguration : IEntityTypeConfiguration<Citation>
{
    public void Configure(EntityTypeBuilder<Citation> builder)
    {
        builder.ToTable("citations");

        builder.HasKey(c => c.Id)
            .HasName("PK_citations");

        builder.Property(c => c.Id)
            .HasColumnName("citation_id");

        builder.Property(c => c.ChatInteractionId)
            .HasColumnName("chat_interaction_id")
            .IsRequired();

        builder.Property(c => c.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(c => c.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.Property(c => c.ChunkId)
            .HasColumnName("chunk_id")
            .IsRequired();

        builder.Property(c => c.Rank)
            .HasColumnName("rank")
            .IsRequired();

        builder.Property(c => c.DocumentTitle)
            .HasColumnName("document_title")
            .HasMaxLength(Citation.DocumentTitleMaxLength)
            .IsRequired();

        builder.Property(c => c.PageNumber)
            .HasColumnName("page_number");

        builder.Property(c => c.SectionLabel)
            .HasColumnName("section_label")
            .HasMaxLength(Citation.SectionLabelMaxLength);

        builder.Property(c => c.RelevanceScore)
            .HasColumnName("relevance_score")
            .HasColumnType("float");

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.HasOne<ChatInteraction>()
            .WithMany()
            .HasForeignKey(c => c.ChatInteractionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_citations_chat_interactions_chat_interaction_id");

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_citations_documents_document_id");

        builder.HasOne<DocumentChunk>()
            .WithMany()
            .HasForeignKey(c => c.ChunkId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_citations_document_chunks_chunk_id");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_citations_organizations_organization_id");

        builder.HasIndex(c => c.ChatInteractionId)
            .HasDatabaseName("IX_citations_chat_interaction_id");

        builder.HasIndex(new[] { nameof(Citation.OrganizationId), nameof(Citation.ChatInteractionId) })
            .HasDatabaseName("IX_citations_organization_id_chat_interaction_id");

        builder.HasIndex(c => c.DocumentId)
            .HasDatabaseName("IX_citations_document_id");
    }
}

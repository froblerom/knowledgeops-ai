using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeOps.Infrastructure.Persistence.Configurations;

internal sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.HasKey(c => c.Id)
            .HasName("PK_document_chunks");

        builder.Property(c => c.Id)
            .HasColumnName("chunk_id");

        builder.Property(c => c.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.Property(c => c.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(c => c.ChunkIndex)
            .HasColumnName("chunk_index")
            .IsRequired();

        builder.Property(c => c.Text)
            .HasColumnName("text")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(c => c.PageNumber)
            .HasColumnName("page_number");

        builder.Property(c => c.SectionLabel)
            .HasColumnName("section_label")
            .HasMaxLength(300);

        builder.Property(c => c.CharacterLength)
            .HasColumnName("character_length");

        builder.Property(c => c.TokenEstimate)
            .HasColumnName("token_estimate");

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.Property(c => c.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Optional);

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_document_chunks_documents_document_id");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_document_chunks_organizations_organization_id");

        builder.HasIndex(c => c.DocumentId)
            .HasDatabaseName("IX_document_chunks_document_id");

        builder.HasIndex(c => c.OrganizationId)
            .HasDatabaseName("IX_document_chunks_organization_id");

        builder.HasIndex(c => new { c.DocumentId, c.ChunkIndex })
            .IsUnique()
            .HasDatabaseName("UX_document_chunks_document_index");

        builder.HasIndex(c => c.DeletedAt)
            .HasDatabaseName("IX_document_chunks_deleted_at");
    }
}

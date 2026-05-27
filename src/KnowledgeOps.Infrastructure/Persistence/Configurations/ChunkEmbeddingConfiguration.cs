using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeOps.Infrastructure.Persistence.Configurations;

internal sealed class ChunkEmbeddingConfiguration : IEntityTypeConfiguration<ChunkEmbedding>
{
    public void Configure(EntityTypeBuilder<ChunkEmbedding> builder)
    {
        builder.ToTable("chunk_embeddings");

        builder.HasKey(e => e.Id)
            .HasName("PK_chunk_embeddings");

        builder.Property(e => e.Id)
            .HasColumnName("chunk_embedding_id");

        builder.Property(e => e.ChunkId)
            .HasColumnName("chunk_id")
            .IsRequired();

        builder.Property(e => e.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(e => e.ProviderName)
            .HasColumnName("provider_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ModelName)
            .HasColumnName("model_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.VectorData)
            .HasColumnName("vector_data")
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.VectorDimensions)
            .HasColumnName("vector_dimensions");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(1000);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.HasOne<DocumentChunk>()
            .WithMany()
            .HasForeignKey(e => e.ChunkId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_chunk_embeddings_document_chunks_chunk_id");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_chunk_embeddings_organizations_organization_id");

        builder.HasIndex(e => e.ChunkId)
            .IsUnique()
            .HasDatabaseName("UX_chunk_embeddings_chunk_id");

        builder.HasIndex(e => e.OrganizationId)
            .HasDatabaseName("IX_chunk_embeddings_organization_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_chunk_embeddings_status");

        builder.HasIndex(e => new { e.ProviderName, e.ModelName })
            .HasDatabaseName("IX_chunk_embeddings_provider_model");
    }
}

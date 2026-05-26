using KnowledgeOps.Domain.Documents;
using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeOps.Infrastructure.Persistence.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents", table =>
        {
            table.HasCheckConstraint(
                "CK_documents_processing_status",
                "[processing_status] IN (N'Uploaded', N'Processing', N'Processed', N'Failed')");
        });

        builder.HasKey(doc => doc.Id)
            .HasName("PK_documents");

        builder.Property(doc => doc.Id)
            .HasColumnName("document_id");

        builder.Property(doc => doc.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(doc => doc.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(doc => doc.Title)
            .HasColumnName("title")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(doc => doc.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(doc => doc.FileSizeBytes)
            .HasColumnName("file_size_bytes")
            .IsRequired();

        builder.Property(doc => doc.StorageLocation)
            .HasColumnName("storage_location")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(doc => doc.ProcessingStatus)
            .HasColumnName("processing_status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(doc => doc.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(Document.FailureReasonMaxLength);

        builder.Property(doc => doc.IsRetrievalEnabled)
            .HasColumnName("is_retrieval_enabled")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(doc => doc.UploadedByUserId)
            .HasColumnName("uploaded_by_user_id")
            .IsRequired();

        builder.Property(doc => doc.UploadedAt)
            .HasColumnName("uploaded_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.Property(doc => doc.ProcessingStartedAt)
            .HasColumnName("processing_started_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Optional);

        builder.Property(doc => doc.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Optional);

        builder.Property(doc => doc.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.Property(doc => doc.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.Property(doc => doc.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Optional);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(doc => doc.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_documents_organizations_organization_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(doc => doc.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_documents_users_uploaded_by_user_id");

        builder.HasIndex(doc => doc.OrganizationId)
            .HasDatabaseName("IX_documents_organization_id");

        builder.HasIndex(doc => doc.UploadedByUserId)
            .HasDatabaseName("IX_documents_uploaded_by_user_id");

        builder.HasIndex(doc => doc.ProcessingStatus)
            .HasDatabaseName("IX_documents_processing_status");

        builder.HasIndex(doc => new { doc.OrganizationId, doc.ProcessingStatus, doc.IsRetrievalEnabled })
            .HasDatabaseName("IX_documents_retrieval_eligibility");

        builder.HasIndex(doc => doc.DeletedAt)
            .HasDatabaseName("IX_documents_deleted_at");

        builder.HasIndex(doc => doc.UploadedAt)
            .HasDatabaseName("IX_documents_uploaded_at");
    }
}

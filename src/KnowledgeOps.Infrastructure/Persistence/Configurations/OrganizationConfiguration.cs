using KnowledgeOps.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeOps.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations", table =>
        {
            table.HasCheckConstraint(
                "CK_organizations_name_not_empty",
                "LEN(LTRIM(RTRIM([name]))) > 0");
            table.HasCheckConstraint(
                "CK_organizations_status",
                "[status] IN (N'Active', N'Disabled')");
        });

        builder.HasKey(organization => organization.Id)
            .HasName("PK_organizations");

        builder.Property(organization => organization.Id)
            .HasColumnName("organization_id");

        builder.Property(organization => organization.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(organization => organization.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(organization => organization.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.Property(organization => organization.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.HasIndex(organization => organization.Name)
            .HasDatabaseName("IX_organizations_name");

        builder.HasIndex(organization => organization.Status)
            .HasDatabaseName("IX_organizations_status");
    }
}

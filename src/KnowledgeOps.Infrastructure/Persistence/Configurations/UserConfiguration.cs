using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeOps.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table =>
        {
            table.HasCheckConstraint(
                "CK_users_status",
                "[status] IN (N'Pending', N'Active', N'Disabled')");
        });

        builder.HasKey(user => user.Id)
            .HasName("PK_users");

        builder.Property(user => user.Id)
            .HasColumnName("user_id");

        builder.Property(user => user.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(user => user.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash");

        builder.Property(user => user.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(user => user.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Optional);

        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.Property(user => user.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.Property(user => user.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Optional);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(user => user.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_users_organizations_organization_id");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("UX_users_email");

        builder.HasIndex(user => user.OrganizationId)
            .HasDatabaseName("IX_users_organization_id");

        builder.HasIndex(user => user.Status)
            .HasDatabaseName("IX_users_status");

        builder.HasIndex(user => user.DeletedAt)
            .HasDatabaseName("IX_users_deleted_at");
    }
}

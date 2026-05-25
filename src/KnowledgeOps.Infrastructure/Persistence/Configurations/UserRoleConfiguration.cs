using KnowledgeOps.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeOps.Infrastructure.Persistence.Configurations;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles", table =>
        {
            table.HasCheckConstraint(
                "CK_user_roles_role_name",
                "[role_name] IN (N'Agent', N'Supervisor', N'KnowledgeAdmin', N'Manager', N'Admin')");
        });

        builder.HasKey(userRole => userRole.Id)
            .HasName("PK_user_roles");

        builder.Property(userRole => userRole.Id)
            .HasColumnName("user_role_id");

        builder.Property(userRole => userRole.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(userRole => userRole.RoleName)
            .HasColumnName("role_name")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(userRole => userRole.AssignedAt)
            .HasColumnName("assigned_at")
            .HasColumnType("datetime2")
            .HasConversion(UtcDateTimeOffsetConverter.Required)
            .IsRequired();

        builder.Property(userRole => userRole.AssignedByUserId)
            .HasColumnName("assigned_by_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_user_roles_users_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(userRole => userRole.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_user_roles_users_assigned_by_user_id");

        builder.HasIndex(userRole => userRole.UserId)
            .HasDatabaseName("IX_user_roles_user_id");

        builder.HasIndex(userRole => userRole.AssignedByUserId)
            .HasDatabaseName("IX_user_roles_assigned_by_user_id");

        builder.HasIndex(userRole => new { userRole.UserId, userRole.RoleName })
            .IsUnique()
            .HasDatabaseName("UX_user_roles_user_role");

        builder.HasIndex(userRole => userRole.RoleName)
            .HasDatabaseName("IX_user_roles_role_name");
    }
}

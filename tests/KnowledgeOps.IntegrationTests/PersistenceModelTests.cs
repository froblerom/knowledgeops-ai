using KnowledgeOps.Domain.Audit;
using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KnowledgeOps.IntegrationTests;

public sealed class PersistenceModelTests
{
    [Fact]
    public void Model_Contains_Only_Approved_Foundation_Tables()
    {
        using var context = CreateContext();

        var tableNames = context.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName()!)
            .OrderBy(tableName => tableName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["audit_log_entries", "organizations", "user_roles", "users"],
            tableNames);
    }

    [Fact]
    public void Model_Maps_Organization_Scope_Keys_And_Canonical_Indexes()
    {
        using var context = CreateContext();

        var userEntity = context.Model.FindEntityType(typeof(User))!;
        var roleEntity = context.Model.FindEntityType(typeof(UserRole))!;
        var auditEntity = context.Model.FindEntityType(typeof(AuditLogEntry))!;

        Assert.Contains(
            userEntity.GetIndexes(),
            index => index.GetDatabaseName() == "UX_users_email" && index.IsUnique);
        Assert.Contains(
            roleEntity.GetIndexes(),
            index => index.GetDatabaseName() == "UX_user_roles_user_role" && index.IsUnique);
        Assert.Contains(
            auditEntity.GetIndexes(),
            index => index.GetDatabaseName() == "IX_audit_log_entries_organization_id");

        Assert.Contains(
            userEntity.GetForeignKeys(),
            key => key.Properties.Single().Name == nameof(User.OrganizationId)
                && key.PrincipalEntityType.ClrType == typeof(Organization));
        Assert.Contains(
            auditEntity.GetForeignKeys(),
            key => key.Properties.Single().Name == nameof(AuditLogEntry.OrganizationId)
                && key.PrincipalEntityType.ClrType == typeof(Organization));
    }

    [Fact]
    public void Model_Stores_Role_Names_Readably_And_Constrains_Mvp_Values()
    {
        using var context = CreateContext();

        var roleProperty = context.Model.FindEntityType(typeof(UserRole))!
            .FindProperty(nameof(UserRole.RoleName))!;
        var converter = roleProperty.GetTypeMapping().Converter!;
        var constraintEntity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(UserRole))!;
        var constraint = constraintEntity.GetCheckConstraints()
            .Single(checkConstraint => checkConstraint.Name == "CK_user_roles_role_name");

        Assert.Equal("KnowledgeAdmin", converter.ConvertToProvider(UserRoleName.KnowledgeAdmin));
        Assert.Contains("N'Agent'", constraint.Sql, StringComparison.Ordinal);
        Assert.Contains("N'Supervisor'", constraint.Sql, StringComparison.Ordinal);
        Assert.Contains("N'KnowledgeAdmin'", constraint.Sql, StringComparison.Ordinal);
        Assert.Contains("N'Manager'", constraint.Sql, StringComparison.Ordinal);
        Assert.Contains("N'Admin'", constraint.Sql, StringComparison.Ordinal);
    }

    private static KnowledgeOpsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<KnowledgeOpsDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=KnowledgeOpsModelOnly;" +
                "Integrated Security=True;TrustServerCertificate=True;Encrypt=True")
            .Options;

        return new KnowledgeOpsDbContext(options);
    }
}

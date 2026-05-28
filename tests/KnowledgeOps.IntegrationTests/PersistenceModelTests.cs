using KnowledgeOps.Domain.Audit;
using KnowledgeOps.Domain.Documents;
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
    public void Model_Contains_Approved_Foundation_And_Document_Metadata_Tables()
    {
        using var context = CreateContext();

        var tableNames = context.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName()!)
            .OrderBy(tableName => tableName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["audit_log_entries", "chunk_embeddings", "document_chunks", "documents", "organizations", "user_roles", "users"],
            tableNames);
    }

    [Fact]
    public void Model_DoesNotIntroduce_RetrievalQueryOrResultPersistence()
    {
        using var context = CreateContext();

        var tableNames = context.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName()!)
            .ToArray();
        var dbSetNames = typeof(KnowledgeOpsDbContext)
            .GetProperties()
            .Where(property =>
                property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("retrieval_results", tableNames);
        Assert.DoesNotContain("retrieval_queries", tableNames);
        Assert.DoesNotContain(dbSetNames, name => name.Contains("RetrievalResult", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dbSetNames, name => name.Contains("RetrievalQuery", StringComparison.OrdinalIgnoreCase));
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

    [Fact]
    public void Model_Maps_Canonical_Document_Metadata_Default_Indexes_And_Status_Constraint()
    {
        using var context = CreateContext();

        var documentEntity = context.Model.FindEntityType(typeof(Document))!;
        var documentDesignEntity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Document))!;
        var constraint = documentDesignEntity.GetCheckConstraints()
            .Single(checkConstraint => checkConstraint.Name == "CK_documents_processing_status");
        var indexNames = documentEntity.GetIndexes()
            .Select(index => index.GetDatabaseName())
            .ToArray();

        Assert.False(documentEntity.FindProperty(nameof(Document.FileName))!.IsNullable);
        Assert.Equal(500, documentEntity.FindProperty(nameof(Document.FileName))!.GetMaxLength());
        Assert.False(documentEntity.FindProperty(nameof(Document.Title))!.IsNullable);
        Assert.Equal(300, documentEntity.FindProperty(nameof(Document.Title))!.GetMaxLength());
        Assert.False(documentEntity.FindProperty(nameof(Document.ContentType))!.IsNullable);
        Assert.Equal(150, documentEntity.FindProperty(nameof(Document.ContentType))!.GetMaxLength());
        Assert.False(documentEntity.FindProperty(nameof(Document.FileSizeBytes))!.IsNullable);
        Assert.False(documentEntity.FindProperty(nameof(Document.StorageLocation))!.IsNullable);
        Assert.Equal(1000, documentEntity.FindProperty(nameof(Document.StorageLocation))!.GetMaxLength());
        Assert.True(documentEntity.FindProperty(nameof(Document.FailureReason))!.IsNullable);
        Assert.Equal(1000, documentEntity.FindProperty(nameof(Document.FailureReason))!.GetMaxLength());
        Assert.False(documentEntity.FindProperty(nameof(Document.UploadedAt))!.IsNullable);
        Assert.True(documentEntity.FindProperty(nameof(Document.ProcessingStartedAt))!.IsNullable);
        Assert.True(documentEntity.FindProperty(nameof(Document.ProcessedAt))!.IsNullable);
        Assert.Equal(false, documentEntity.FindProperty(nameof(Document.IsRetrievalEnabled))!.GetDefaultValue());

        Assert.Contains("IX_documents_organization_id", indexNames);
        Assert.Contains("IX_documents_uploaded_by_user_id", indexNames);
        Assert.Contains("IX_documents_processing_status", indexNames);
        Assert.Contains("IX_documents_retrieval_eligibility", indexNames);
        Assert.Contains("IX_documents_deleted_at", indexNames);
        Assert.Contains("IX_documents_uploaded_at", indexNames);
        Assert.Contains("N'Uploaded'", constraint.Sql, StringComparison.Ordinal);
        Assert.Contains("N'Processing'", constraint.Sql, StringComparison.Ordinal);
        Assert.Contains("N'Processed'", constraint.Sql, StringComparison.Ordinal);
        Assert.Contains("N'Failed'", constraint.Sql, StringComparison.Ordinal);
        Assert.DoesNotContain("Disabled", constraint.Sql, StringComparison.Ordinal);

        Assert.Contains(
            documentEntity.GetForeignKeys(),
            key => key.Properties.Single().Name == nameof(Document.OrganizationId)
                && key.PrincipalEntityType.ClrType == typeof(Organization));
        Assert.Contains(
            documentEntity.GetForeignKeys(),
            key => key.Properties.Single().Name == nameof(Document.UploadedByUserId)
                && key.PrincipalEntityType.ClrType == typeof(User));
    }

    [Fact]
    public void Model_Maps_DocumentChunks_Schema_Indexes_And_ForeignKeys()
    {
        using var context = CreateContext();

        var chunkEntity = context.Model.FindEntityType(typeof(DocumentChunk))!;
        var indexNames = chunkEntity.GetIndexes()
            .Select(index => index.GetDatabaseName())
            .ToArray();

        Assert.Equal("document_chunks", chunkEntity.GetTableName());
        Assert.False(chunkEntity.FindProperty(nameof(DocumentChunk.Text))!.IsNullable);
        Assert.True(chunkEntity.FindProperty(nameof(DocumentChunk.PageNumber))!.IsNullable);
        Assert.True(chunkEntity.FindProperty(nameof(DocumentChunk.SectionLabel))!.IsNullable);
        Assert.Equal(300, chunkEntity.FindProperty(nameof(DocumentChunk.SectionLabel))!.GetMaxLength());
        Assert.True(chunkEntity.FindProperty(nameof(DocumentChunk.DeletedAt))!.IsNullable);

        Assert.Contains("IX_document_chunks_document_id", indexNames);
        Assert.Contains("IX_document_chunks_organization_id", indexNames);
        Assert.Contains("IX_document_chunks_deleted_at", indexNames);
        Assert.Contains(
            chunkEntity.GetIndexes(),
            idx => idx.GetDatabaseName() == "UX_document_chunks_document_index" && idx.IsUnique);

        Assert.Contains(
            chunkEntity.GetForeignKeys(),
            key => key.Properties.Single().Name == nameof(DocumentChunk.DocumentId)
                && key.PrincipalEntityType.ClrType == typeof(Document));
        Assert.Contains(
            chunkEntity.GetForeignKeys(),
            key => key.Properties.Single().Name == nameof(DocumentChunk.OrganizationId)
                && key.PrincipalEntityType.ClrType == typeof(Organization));
    }

    [Fact]
    public void Model_Maps_ChunkEmbeddings_Schema_Indexes_And_ForeignKeys()
    {
        using var context = CreateContext();

        var embeddingEntity = context.Model.FindEntityType(typeof(ChunkEmbedding))!;
        var indexNames = embeddingEntity.GetIndexes()
            .Select(index => index.GetDatabaseName())
            .ToArray();

        Assert.Equal("chunk_embeddings", embeddingEntity.GetTableName());
        Assert.False(embeddingEntity.FindProperty(nameof(ChunkEmbedding.ProviderName))!.IsNullable);
        Assert.Equal(100, embeddingEntity.FindProperty(nameof(ChunkEmbedding.ProviderName))!.GetMaxLength());
        Assert.False(embeddingEntity.FindProperty(nameof(ChunkEmbedding.ModelName))!.IsNullable);
        Assert.Equal(150, embeddingEntity.FindProperty(nameof(ChunkEmbedding.ModelName))!.GetMaxLength());
        Assert.True(embeddingEntity.FindProperty(nameof(ChunkEmbedding.VectorData))!.IsNullable);
        Assert.True(embeddingEntity.FindProperty(nameof(ChunkEmbedding.VectorDimensions))!.IsNullable);
        Assert.True(embeddingEntity.FindProperty(nameof(ChunkEmbedding.FailureReason))!.IsNullable);
        Assert.Equal(1000, embeddingEntity.FindProperty(nameof(ChunkEmbedding.FailureReason))!.GetMaxLength());
        Assert.True(embeddingEntity.FindProperty(nameof(ChunkEmbedding.IndexStatus))!.IsNullable);
        Assert.Equal(50, embeddingEntity.FindProperty(nameof(ChunkEmbedding.IndexStatus))!.GetMaxLength());
        Assert.True(embeddingEntity.FindProperty(nameof(ChunkEmbedding.IndexedAt))!.IsNullable);
        Assert.True(embeddingEntity.FindProperty(nameof(ChunkEmbedding.IndexFailureReason))!.IsNullable);
        Assert.Equal(1000, embeddingEntity.FindProperty(nameof(ChunkEmbedding.IndexFailureReason))!.GetMaxLength());

        Assert.Contains("IX_chunk_embeddings_organization_id", indexNames);
        Assert.Contains("IX_chunk_embeddings_organization_index_status", indexNames);
        Assert.Contains("IX_chunk_embeddings_status", indexNames);
        Assert.Contains("IX_chunk_embeddings_provider_model", indexNames);
        Assert.Contains(
            embeddingEntity.GetIndexes(),
            idx => idx.GetDatabaseName() == "UX_chunk_embeddings_chunk_id" && idx.IsUnique);

        Assert.Contains(
            embeddingEntity.GetForeignKeys(),
            key => key.Properties.Single().Name == nameof(ChunkEmbedding.ChunkId)
                && key.PrincipalEntityType.ClrType == typeof(DocumentChunk));
        Assert.Contains(
            embeddingEntity.GetForeignKeys(),
            key => key.Properties.Single().Name == nameof(ChunkEmbedding.OrganizationId)
                && key.PrincipalEntityType.ClrType == typeof(Organization));
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

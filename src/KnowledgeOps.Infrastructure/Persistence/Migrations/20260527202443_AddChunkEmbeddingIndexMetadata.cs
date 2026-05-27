using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkEmbeddingIndexMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "index_failure_reason",
                table: "chunk_embeddings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "index_status",
                table: "chunk_embeddings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "indexed_at",
                table: "chunk_embeddings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_chunk_embeddings_organization_index_status",
                table: "chunk_embeddings",
                columns: new[] { "organization_id", "index_status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_chunk_embeddings_organization_index_status",
                table: "chunk_embeddings");

            migrationBuilder.DropColumn(
                name: "index_failure_reason",
                table: "chunk_embeddings");

            migrationBuilder.DropColumn(
                name: "index_status",
                table: "chunk_embeddings");

            migrationBuilder.DropColumn(
                name: "indexed_at",
                table: "chunk_embeddings");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChunkEmbeddingsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chunk_embeddings",
                columns: table => new
                {
                    chunk_embedding_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    chunk_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    model_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    vector_data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vector_dimensions = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    failure_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chunk_embeddings", x => x.chunk_embedding_id);
                    table.ForeignKey(
                        name: "FK_chunk_embeddings_document_chunks_chunk_id",
                        column: x => x.chunk_id,
                        principalTable: "document_chunks",
                        principalColumn: "chunk_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chunk_embeddings_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chunk_embeddings_organization_id",
                table: "chunk_embeddings",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_chunk_embeddings_provider_model",
                table: "chunk_embeddings",
                columns: new[] { "provider_name", "model_name" });

            migrationBuilder.CreateIndex(
                name: "IX_chunk_embeddings_status",
                table: "chunk_embeddings",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "UX_chunk_embeddings_chunk_id",
                table: "chunk_embeddings",
                column: "chunk_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chunk_embeddings");
        }
    }
}

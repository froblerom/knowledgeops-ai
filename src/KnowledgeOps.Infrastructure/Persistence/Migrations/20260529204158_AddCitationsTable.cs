using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCitationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "citations",
                columns: table => new
                {
                    citation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    chat_interaction_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    document_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    chunk_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rank = table.Column<int>(type: "int", nullable: false),
                    document_title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    page_number = table.Column<int>(type: "int", nullable: true),
                    section_label = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    relevance_score = table.Column<double>(type: "float", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_citations", x => x.citation_id);
                    table.ForeignKey(
                        name: "FK_citations_chat_interactions_chat_interaction_id",
                        column: x => x.chat_interaction_id,
                        principalTable: "chat_interactions",
                        principalColumn: "chat_interaction_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_citations_document_chunks_chunk_id",
                        column: x => x.chunk_id,
                        principalTable: "document_chunks",
                        principalColumn: "chunk_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_citations_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_citations_chat_interaction_id",
                table: "citations",
                column: "chat_interaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_citations_chunk_id",
                table: "citations",
                column: "chunk_id");

            migrationBuilder.CreateIndex(
                name: "IX_citations_document_id",
                table: "citations",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_citations_organization_id_chat_interaction_id",
                table: "citations",
                columns: new[] { "organization_id", "chat_interaction_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "citations");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChatSessionStatusAndCitationDocumentFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "chat_sessions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddForeignKey(
                name: "FK_citations_documents_document_id",
                table: "citations",
                column: "document_id",
                principalTable: "documents",
                principalColumn: "document_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_citations_documents_document_id",
                table: "citations");

            migrationBuilder.DropColumn(
                name: "status",
                table: "chat_sessions");
        }
    }
}

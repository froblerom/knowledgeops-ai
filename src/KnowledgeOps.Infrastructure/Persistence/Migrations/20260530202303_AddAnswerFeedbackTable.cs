using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerFeedbackTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "answer_feedback",
                columns: table => new
                {
                    answer_feedback_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    chat_interaction_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rating = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_answer_feedback", x => x.answer_feedback_id);
                    table.CheckConstraint("CK_answer_feedback_rating", "[rating] IN (N'Useful', N'NotUseful')");
                    table.ForeignKey(
                        name: "FK_answer_feedback_chat_interactions_chat_interaction_id",
                        column: x => x.chat_interaction_id,
                        principalTable: "chat_interactions",
                        principalColumn: "chat_interaction_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_answer_feedback_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_answer_feedback_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_answer_feedback_chat_interaction_id",
                table: "answer_feedback",
                column: "chat_interaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_answer_feedback_organization_id",
                table: "answer_feedback",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_answer_feedback_organization_id_rating",
                table: "answer_feedback",
                columns: new[] { "organization_id", "rating" });

            migrationBuilder.CreateIndex(
                name: "IX_answer_feedback_user_id",
                table: "answer_feedback",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UX_answer_feedback_chat_interaction_id_user_id",
                table: "answer_feedback",
                columns: new[] { "chat_interaction_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "answer_feedback");
        }
    }
}

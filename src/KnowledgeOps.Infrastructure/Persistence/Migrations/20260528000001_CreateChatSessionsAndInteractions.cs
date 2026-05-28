using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateChatSessionsAndInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_sessions",
                columns: table => new
                {
                    chat_session_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_interaction_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_sessions", x => x.chat_session_id);
                    table.ForeignKey(
                        name: "FK_chat_sessions_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chat_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chat_interactions",
                columns: table => new
                {
                    chat_interaction_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    chat_session_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    question_text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    question_text_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    answer_text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    answer_state = table.Column<int>(type: "int", nullable: false),
                    retrieval_query_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    retrieval_candidate_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    retrieval_latency_ms = table.Column<long>(type: "bigint", nullable: true),
                    generation_latency_ms = table.Column<long>(type: "bigint", nullable: true),
                    total_latency_ms = table.Column<long>(type: "bigint", nullable: true),
                    ai_provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ai_model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    token_usage_input = table.Column<int>(type: "int", nullable: true),
                    token_usage_output = table.Column<int>(type: "int", nullable: true),
                    estimated_cost = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    provider_failure_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    correlation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    prompt_version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_interactions", x => x.chat_interaction_id);
                    table.ForeignKey(
                        name: "FK_chat_interactions_chat_sessions_chat_session_id",
                        column: x => x.chat_session_id,
                        principalTable: "chat_sessions",
                        principalColumn: "chat_session_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chat_interactions_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chat_interactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_sessions_organization_id",
                table: "chat_sessions",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_sessions_organization_id_created_at",
                table: "chat_sessions",
                columns: new[] { "organization_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_sessions_user_id",
                table: "chat_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_interactions_answer_state",
                table: "chat_interactions",
                column: "answer_state");

            migrationBuilder.CreateIndex(
                name: "IX_chat_interactions_chat_session_id",
                table: "chat_interactions",
                column: "chat_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_interactions_organization_id_created_at",
                table: "chat_interactions",
                columns: new[] { "organization_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_interactions_user_id",
                table: "chat_interactions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_interactions");

            migrationBuilder.DropTable(
                name: "chat_sessions");
        }
    }
}

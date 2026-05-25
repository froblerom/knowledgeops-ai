using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KnowledgeOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedFictionalOrganizationsAndPersonas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "organizations",
                columns: new[] { "organization_id", "created_at", "name", "status", "updated_at" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-4111-8111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Asteria Support Group", "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-2222-4222-8222-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Boreal Contact Services", "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "user_id", "created_at", "deleted_at", "display_name", "email", "last_login_at", "organization_id", "password_hash", "status", "updated_at" },
                values: new object[,]
                {
                    { new Guid("aaaa0001-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Agent A", "agent.a@asteria.example.com", null, new Guid("11111111-1111-4111-8111-111111111111"), null, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("aaaa0002-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Supervisor A", "supervisor.a@asteria.example.com", null, new Guid("11111111-1111-4111-8111-111111111111"), null, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("aaaa0003-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "KnowledgeAdmin A", "knowledgeadmin.a@asteria.example.com", null, new Guid("11111111-1111-4111-8111-111111111111"), null, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("aaaa0004-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Manager A", "manager.a@asteria.example.com", null, new Guid("11111111-1111-4111-8111-111111111111"), null, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("aaaa0005-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Admin A", "admin.a@asteria.example.com", null, new Guid("11111111-1111-4111-8111-111111111111"), null, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("bbbb0001-bbbb-4bbb-8bbb-bbbbbbbbbbbb"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Agent B", "agent.b@boreal.example.com", null, new Guid("22222222-2222-4222-8222-222222222222"), null, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("bbbb0002-bbbb-4bbb-8bbb-bbbbbbbbbbbb"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Admin B", "admin.b@boreal.example.com", null, new Guid("22222222-2222-4222-8222-222222222222"), null, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "user_roles",
                columns: new[] { "user_role_id", "assigned_at", "assigned_by_user_id", "role_name", "user_id" },
                values: new object[,]
                {
                    { new Guid("cccc0001-cccc-4ccc-8ccc-cccccccccccc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Agent", new Guid("aaaa0001-aaaa-4aaa-8aaa-aaaaaaaaaaaa") },
                    { new Guid("cccc0002-cccc-4ccc-8ccc-cccccccccccc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Supervisor", new Guid("aaaa0002-aaaa-4aaa-8aaa-aaaaaaaaaaaa") },
                    { new Guid("cccc0003-cccc-4ccc-8ccc-cccccccccccc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "KnowledgeAdmin", new Guid("aaaa0003-aaaa-4aaa-8aaa-aaaaaaaaaaaa") },
                    { new Guid("cccc0004-cccc-4ccc-8ccc-cccccccccccc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Manager", new Guid("aaaa0004-aaaa-4aaa-8aaa-aaaaaaaaaaaa") },
                    { new Guid("cccc0005-cccc-4ccc-8ccc-cccccccccccc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Admin", new Guid("aaaa0005-aaaa-4aaa-8aaa-aaaaaaaaaaaa") },
                    { new Guid("cccc0006-cccc-4ccc-8ccc-cccccccccccc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Agent", new Guid("bbbb0001-bbbb-4bbb-8bbb-bbbbbbbbbbbb") },
                    { new Guid("cccc0007-cccc-4ccc-8ccc-cccccccccccc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Admin", new Guid("bbbb0002-bbbb-4bbb-8bbb-bbbbbbbbbbbb") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumn: "user_role_id",
                keyValue: new Guid("cccc0001-cccc-4ccc-8ccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumn: "user_role_id",
                keyValue: new Guid("cccc0002-cccc-4ccc-8ccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumn: "user_role_id",
                keyValue: new Guid("cccc0003-cccc-4ccc-8ccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumn: "user_role_id",
                keyValue: new Guid("cccc0004-cccc-4ccc-8ccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumn: "user_role_id",
                keyValue: new Guid("cccc0005-cccc-4ccc-8ccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumn: "user_role_id",
                keyValue: new Guid("cccc0006-cccc-4ccc-8ccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumn: "user_role_id",
                keyValue: new Guid("cccc0007-cccc-4ccc-8ccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaa0001-aaaa-4aaa-8aaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaa0002-aaaa-4aaa-8aaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaa0003-aaaa-4aaa-8aaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaa0004-aaaa-4aaa-8aaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaa0005-aaaa-4aaa-8aaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("bbbb0001-bbbb-4bbb-8bbb-bbbbbbbbbbbb"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("bbbb0002-bbbb-4bbb-8bbb-bbbbbbbbbbbb"));

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "organization_id",
                keyValue: new Guid("11111111-1111-4111-8111-111111111111"));

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "organization_id",
                keyValue: new Guid("22222222-2222-4222-8222-222222222222"));
        }
    }
}

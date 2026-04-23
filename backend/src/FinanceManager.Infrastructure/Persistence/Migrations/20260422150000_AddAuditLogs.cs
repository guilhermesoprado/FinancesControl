using System;
using FinanceManager.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceManager.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(FinanceManagerDbContext))]
    [Migration("20260422150000_AddAuditLogs")]
    public partial class AddAuditLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_UserId",
                table: "audit_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_UserId_EntityType_EntityId_CreatedAtUtc",
                table: "audit_logs",
                columns: new[] { "UserId", "EntityType", "EntityId", "CreatedAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");
        }
    }
}

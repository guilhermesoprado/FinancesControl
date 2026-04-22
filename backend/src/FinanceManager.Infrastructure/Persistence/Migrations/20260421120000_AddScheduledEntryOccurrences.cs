using System;
using FinanceManager.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceManager.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(FinanceManagerDbContext))]
    [Migration("20260421120000_AddScheduledEntryOccurrences")]
    public partial class AddScheduledEntryOccurrences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scheduled_entry_occurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurrenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_entry_occurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scheduled_entry_occurrences_scheduled_entries_ScheduledEntryId",
                        column: x => x.ScheduledEntryId,
                        principalTable: "scheduled_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_scheduled_entry_occurrences_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_entry_occurrences_ScheduledEntryId_OccurrenceDate",
                table: "scheduled_entry_occurrences",
                columns: new[] { "ScheduledEntryId", "OccurrenceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_entry_occurrences_UserId_Status_OccurrenceDate",
                table: "scheduled_entry_occurrences",
                columns: new[] { "UserId", "Status", "OccurrenceDate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_entry_occurrences");
        }
    }
}

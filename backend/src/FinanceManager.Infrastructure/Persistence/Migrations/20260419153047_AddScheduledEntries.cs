using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "RevolvingInterestAppliedAmount",
                table: "invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PaidAmount",
                table: "invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "LateInterestAppliedAmount",
                table: "invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "LateFeeAppliedAmount",
                table: "invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateTable(
                name: "scheduled_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PlanningMode = table.Column<int>(type: "integer", nullable: false),
                    RecurrenceFrequency = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NextOccurrenceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastRealizedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scheduled_entries_financial_accounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalTable: "financial_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scheduled_entries_transaction_categories_TransactionCategor~",
                        column: x => x.TransactionCategoryId,
                        principalTable: "transaction_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scheduled_entries_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_entries_FinancialAccountId",
                table: "scheduled_entries",
                column: "FinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_entries_TransactionCategoryId",
                table: "scheduled_entries",
                column: "TransactionCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_entries_UserId_FinancialAccountId",
                table: "scheduled_entries",
                columns: new[] { "UserId", "FinancialAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_entries_UserId_Status_NextOccurrenceDate",
                table: "scheduled_entries",
                columns: new[] { "UserId", "Status", "NextOccurrenceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_entries_UserId_TransactionCategoryId",
                table: "scheduled_entries",
                columns: new[] { "UserId", "TransactionCategoryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_entries");

            migrationBuilder.AlterColumn<decimal>(
                name: "RevolvingInterestAppliedAmount",
                table: "invoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "PaidAmount",
                table: "invoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "LateInterestAppliedAmount",
                table: "invoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "LateFeeAppliedAmount",
                table: "invoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAtUtc",
                table: "invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaidFromFinancialAccountId",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_PaidFromFinancialAccountId",
                table: "invoices",
                column: "PaidFromFinancialAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_financial_accounts_PaidFromFinancialAccountId",
                table: "invoices",
                column: "PaidFromFinancialAccountId",
                principalTable: "financial_accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_financial_accounts_PaidFromFinancialAccountId",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoices_PaidFromFinancialAccountId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "PaidAtUtc",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "PaidFromFinancialAccountId",
                table: "invoices");
        }
    }
}

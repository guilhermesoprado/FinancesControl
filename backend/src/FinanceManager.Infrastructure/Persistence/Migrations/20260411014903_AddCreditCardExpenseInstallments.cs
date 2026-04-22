using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardExpenseInstallments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ChargesAppliedUntilDate",
                table: "invoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAtUtc",
                table: "invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LateFeeAppliedAmount",
                table: "invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LateInterestAppliedAmount",
                table: "invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RevolvingInterestAppliedAmount",
                table: "invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentCount",
                table: "credit_card_expenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "InstallmentGroupId",
                table: "credit_card_expenses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "InstallmentNumber",
                table: "credit_card_expenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_expenses_InstallmentGroupId",
                table: "credit_card_expenses",
                column: "InstallmentGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_credit_card_expenses_InstallmentGroupId",
                table: "credit_card_expenses");

            migrationBuilder.DropColumn(
                name: "ChargesAppliedUntilDate",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "LateFeeAppliedAmount",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "LateInterestAppliedAmount",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "RevolvingInterestAppliedAmount",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "InstallmentCount",
                table: "credit_card_expenses");

            migrationBuilder.DropColumn(
                name: "InstallmentGroupId",
                table: "credit_card_expenses");

            migrationBuilder.DropColumn(
                name: "InstallmentNumber",
                table: "credit_card_expenses");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credit_card_expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_card_expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_credit_card_expenses_credit_cards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "credit_cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_credit_card_expenses_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_credit_card_expenses_transaction_categories_TransactionCate~",
                        column: x => x.TransactionCategoryId,
                        principalTable: "transaction_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_card_expenses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_expenses_CreditCardId",
                table: "credit_card_expenses",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_expenses_InvoiceId",
                table: "credit_card_expenses",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_expenses_TransactionCategoryId",
                table: "credit_card_expenses",
                column: "TransactionCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_expenses_UserId_CreditCardId_OccurredOn",
                table: "credit_card_expenses",
                columns: new[] { "UserId", "CreditCardId", "OccurredOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_card_expenses");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionsCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceFinancialAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationFinancialAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transactions_financial_accounts_DestinationFinancialAccount~",
                        column: x => x.DestinationFinancialAccountId,
                        principalTable: "financial_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_financial_accounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalTable: "financial_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_financial_accounts_SourceFinancialAccountId",
                        column: x => x.SourceFinancialAccountId,
                        principalTable: "financial_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_transaction_categories_TransactionCategoryId",
                        column: x => x.TransactionCategoryId,
                        principalTable: "transaction_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_DestinationFinancialAccountId",
                table: "transactions",
                column: "DestinationFinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_FinancialAccountId",
                table: "transactions",
                column: "FinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_SourceFinancialAccountId",
                table: "transactions",
                column: "SourceFinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_TransactionCategoryId",
                table: "transactions",
                column: "TransactionCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_UserId_OccurredOn",
                table: "transactions",
                columns: new[] { "UserId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_UserId_Type_OccurredOn",
                table: "transactions",
                columns: new[] { "UserId", "Type", "OccurredOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transactions");
        }
    }
}

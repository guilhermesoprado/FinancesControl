using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Tests;

public sealed class TransactionsDomainTests
{
    [Fact]
    public void CreateIncome_ShouldPopulateTransactionWithTrimmedDescription()
    {
        var nowUtc = new DateTime(2026, 4, 8, 12, 30, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var transaction = Transaction.CreateIncome(
            userId,
            accountId,
            categoryId,
            150.75m,
            new DateOnly(2026, 4, 8),
            "  Salario principal  ",
            nowUtc);

        Assert.Equal(userId, transaction.UserId);
        Assert.Equal(TransactionType.Income, transaction.Type);
        Assert.Equal(TransactionStatus.Posted, transaction.Status);
        Assert.Equal(accountId, transaction.FinancialAccountId);
        Assert.Equal(categoryId, transaction.TransactionCategoryId);
        Assert.Null(transaction.SourceFinancialAccountId);
        Assert.Null(transaction.DestinationFinancialAccountId);
        Assert.Equal("Salario principal", transaction.Description);
        Assert.Equal(nowUtc, transaction.CreatedAtUtc);
        Assert.Equal(nowUtc, transaction.UpdatedAtUtc);
    }

    [Fact]
    public void CreateTransfer_ShouldRejectEqualSourceAndDestinationAccounts()
    {
        var accountId = Guid.NewGuid();

        var action = () => Transaction.CreateTransfer(
            Guid.NewGuid(),
            accountId,
            accountId,
            10m,
            new DateOnly(2026, 4, 8),
            null,
            DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("A transferencia exige contas de origem e destino diferentes.", exception.Message);
    }

    [Fact]
    public void ApplyDelta_ShouldUpdateBalanceSnapshotAndTimestamp()
    {
        var createdAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 4, 8, 14, 15, 0, DateTimeKind.Utc);

        var account = FinancialAccount.Create(
            Guid.NewGuid(),
            "Conta principal",
            FinancialAccountType.BankAccount,
            100m,
            "Banco",
            null,
            createdAt);

        account.ApplyDelta(-35.5m, updatedAt);

        Assert.Equal(64.5m, account.CurrentBalanceSnapshot);
        Assert.Equal(updatedAt, account.UpdatedAtUtc);
    }
}

using FinanceManager.Domain.Entities;

namespace FinanceManager.Domain.Tests;

public sealed class CreditCardExpenseDomainTests
{
    [Fact]
    public void Register_ShouldPersistInstallmentMetadata()
    {
        var nowUtc = new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc);
        var groupId = Guid.NewGuid();

        var expense = CreditCardExpense.Register(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            groupId,
            2,
            6,
            99.90m,
            new DateOnly(2026, 4, 9),
            "Compra parcelada",
            nowUtc);

        Assert.Equal(groupId, expense.InstallmentGroupId);
        Assert.Equal(2, expense.InstallmentNumber);
        Assert.Equal(6, expense.InstallmentCount);
        Assert.Equal(99.90m, expense.Amount);
    }
}

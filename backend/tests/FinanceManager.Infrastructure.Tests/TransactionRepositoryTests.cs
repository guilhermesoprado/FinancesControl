using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Persistence.Context;
using FinanceManager.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Tests;

public sealed class TransactionRepositoryTests
{
    [Fact]
    public async Task GetByUserAndPeriodAsync_ShouldFilterByPeriodTypeAndAccount()
    {
        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc);

        await using var dbContext = CreateDbContext();
        var repository = new TransactionRepository(dbContext);

        var matchingTransfer = Transaction.CreateTransfer(userId, accountA, accountB, 30m, new DateOnly(2026, 4, 8), "Transfer", nowUtc.AddMinutes(2));
        var matchingIncome = Transaction.CreateIncome(userId, accountA, categoryId, 120m, new DateOnly(2026, 4, 7), "Income", nowUtc.AddMinutes(1));
        var outOfType = Transaction.CreateExpense(userId, accountA, categoryId, 20m, new DateOnly(2026, 4, 6), "Expense", nowUtc);
        var anotherUser = Transaction.CreateIncome(anotherUserId, accountA, categoryId, 999m, new DateOnly(2026, 4, 8), "Other", nowUtc.AddMinutes(3));

        await repository.AddAsync(matchingTransfer, CancellationToken.None);
        await repository.AddAsync(matchingIncome, CancellationToken.None);
        await repository.AddAsync(outOfType, CancellationToken.None);
        await repository.AddAsync(anotherUser, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var results = await repository.GetByUserAndPeriodAsync(
            userId,
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30),
            null,
            accountA,
            CancellationToken.None);

        Assert.Equal(3, results.Count);
        Assert.Equal(matchingTransfer.Id, results[0].Id);
        Assert.Equal(matchingIncome.Id, results[1].Id);
        Assert.Equal(outOfType.Id, results[2].Id);

        var incomeOnly = await repository.GetByUserAndPeriodAsync(
            userId,
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30),
            TransactionType.Income,
            accountA,
            CancellationToken.None);

        var single = Assert.Single(incomeOnly);
        Assert.Equal(matchingIncome.Id, single.Id);
    }

    private static FinanceManagerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceManagerDbContext>()
            .UseInMemoryDatabase($"transactions-tests-{Guid.NewGuid()}")
            .Options;

        return new FinanceManagerDbContext(options);
    }
}

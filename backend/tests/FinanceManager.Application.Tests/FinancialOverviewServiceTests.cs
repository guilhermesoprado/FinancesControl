using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.FinancialOverview.Services;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Tests;

public sealed class FinancialOverviewServiceTests
{
    [Fact]
    public async Task GetAsync_ShouldAggregateAccountsAndCurrentMonthTransactions()
    {
        var nowUtc = new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var accountA = FinancialAccount.Create(
            userId,
            "Conta principal",
            FinancialAccountType.BankAccount,
            100m,
            null,
            null,
            nowUtc.AddDays(-20));
        var accountB = FinancialAccount.Create(
            userId,
            "Reserva",
            FinancialAccountType.InvestmentAccount,
            50m,
            null,
            null,
            nowUtc.AddDays(-20));

        accountA.ApplyDelta(25m, nowUtc.AddDays(-1));
        accountB.ApplyDelta(10m, nowUtc.AddDays(-1));

        var incomeCategory = TransactionCategory.CreateUserCategory(
            userId,
            "Salario",
            TransactionCategoryType.Income,
            "#22c55e",
            null,
            nowUtc.AddDays(-20));
        var expenseCategory = TransactionCategory.CreateUserCategory(
            userId,
            "Mercado",
            TransactionCategoryType.Expense,
            "#ef4444",
            null,
            nowUtc.AddDays(-20));

        var transactions = new List<Transaction>
        {
            Transaction.CreateIncome(userId, accountA.Id, incomeCategory.Id, 120m, new DateOnly(2026, 4, 8), "Salario", nowUtc.AddMinutes(-1)),
            Transaction.CreateExpense(userId, accountA.Id, expenseCategory.Id, 30m, new DateOnly(2026, 4, 7), "Mercado", nowUtc.AddMinutes(-2)),
            Transaction.CreateTransfer(userId, accountA.Id, accountB.Id, 15m, new DateOnly(2026, 4, 6), "Reserva", nowUtc.AddMinutes(-3)),
            Transaction.CreateIncome(userId, accountA.Id, incomeCategory.Id, 90m, new DateOnly(2026, 3, 8), "Salario anterior", nowUtc.AddDays(-20)),
            Transaction.CreateExpense(userId, accountA.Id, expenseCategory.Id, 45m, new DateOnly(2026, 3, 7), "Mercado anterior", nowUtc.AddDays(-21)),
        };

        var service = new FinancialOverviewService(
            new FakeFinancialAccountRepository(accountA, accountB),
            new FakeTransactionCategoryRepository(incomeCategory, expenseCategory),
            new FakeTransactionRepository(transactions),
            new FakeDateTimeProvider(nowUtc));

        var overview = await service.GetAsync(userId, CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 4, 1), overview.PeriodFrom);
        Assert.Equal(new DateOnly(2026, 4, 9), overview.PeriodTo);
        Assert.Equal(185m, overview.ConsolidatedBalance);
        Assert.Equal(2, overview.ActiveAccountsCount);
        Assert.Equal(120m, overview.IncomeTotal);
        Assert.Equal(30m, overview.ExpenseTotal);
        Assert.Equal(15m, overview.TransferTotal);
        Assert.Equal(new DateOnly(2026, 3, 1), overview.PeriodComparison.PreviousPeriodFrom);
        Assert.Equal(new DateOnly(2026, 3, 9), overview.PeriodComparison.PreviousPeriodTo);
        Assert.Equal(90m, overview.PeriodComparison.PreviousIncomeTotal);
        Assert.Equal(45m, overview.PeriodComparison.PreviousExpenseTotal);
        Assert.Equal(45m, overview.PeriodComparison.PreviousNetResult);
        Assert.Equal(2, overview.Accounts.Count);
        Assert.Equal(3, overview.RecentTransactions.Count);
        Assert.Single(overview.AccountSummaries);
        Assert.Equal("Conta principal", overview.AccountSummaries[0].AccountName);
        Assert.Equal(90m, overview.AccountSummaries[0].NetResult);
        Assert.Equal(2, overview.CategorySummaries.Count);
        Assert.Contains(overview.CategorySummaries, x => x.CategoryName == "Salario" && x.TotalAmount == 120m);
        Assert.Contains(overview.CategorySummaries, x => x.CategoryName == "Mercado" && x.TotalAmount == 30m);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class FakeFinancialAccountRepository : IFinancialAccountRepository
    {
        private readonly IReadOnlyList<FinancialAccount> _accounts;

        public FakeFinancialAccountRepository(params FinancialAccount[] accounts)
        {
            _accounts = accounts;
        }

        public Task AddAsync(FinancialAccount financialAccount, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<FinancialAccount?> GetByUserIdAndIdAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_accounts.FirstOrDefault(x => x.UserId == userId && x.Id == financialAccountId));
        }

        public Task<IReadOnlyList<FinancialAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<FinancialAccount>>(_accounts.Where(x => x.UserId == userId).ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        private readonly IReadOnlyList<Transaction> _transactions;

        public FakeTransactionRepository(IReadOnlyList<Transaction> transactions)
        {
            _transactions = transactions;
        }

        public Task AddAsync(Transaction transaction, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<Transaction>> GetByUserAndPeriodAsync(
            Guid userId,
            DateOnly from,
            DateOnly to,
            TransactionType? type,
            Guid? financialAccountId,
            CancellationToken cancellationToken)
        {
            var query = _transactions.Where(x => x.UserId == userId && x.OccurredOn >= from && x.OccurredOn <= to);

            if (type.HasValue)
            {
                query = query.Where(x => x.Type == type.Value);
            }

            if (financialAccountId.HasValue)
            {
                var accountId = financialAccountId.Value;
                query = query.Where(x =>
                    x.FinancialAccountId == accountId ||
                    x.SourceFinancialAccountId == accountId ||
                    x.DestinationFinancialAccountId == accountId);
            }

            return Task.FromResult<IReadOnlyList<Transaction>>(query.OrderByDescending(x => x.OccurredOn).ThenByDescending(x => x.CreatedAtUtc).ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTransactionCategoryRepository : ITransactionCategoryRepository
    {
        private readonly IReadOnlyList<TransactionCategory> _categories;

        public FakeTransactionCategoryRepository(params TransactionCategory[] categories)
        {
            _categories = categories;
        }

        public Task AddAsync(TransactionCategory transactionCategory, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> ExistsByUserAndNameAndTypeAsync(Guid userId, string normalizedName, TransactionCategoryType type, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<TransactionCategory?> GetByUserIdAndIdAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_categories.FirstOrDefault(x => x.UserId == userId && x.Id == transactionCategoryId));
        }

        public Task<IReadOnlyList<TransactionCategory>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<TransactionCategory>>(_categories.Where(x => x.UserId == userId).ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

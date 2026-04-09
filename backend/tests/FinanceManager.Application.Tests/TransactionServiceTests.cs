using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.Transactions.Contracts;
using FinanceManager.Application.Transactions.Services;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Tests;

public sealed class TransactionServiceTests
{
    [Fact]
    public async Task RegisterIncomeAsync_ShouldPersistTransactionAndIncreaseAccountBalance()
    {
        var nowUtc = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var account = FinancialAccount.Create(
            userId,
            "Conta corrente",
            FinancialAccountType.BankAccount,
            100m,
            null,
            null,
            nowUtc.AddDays(-7));
        var category = TransactionCategory.CreateUserCategory(
            userId,
            "Salario",
            TransactionCategoryType.Income,
            null,
            null,
            nowUtc.AddDays(-6));

        var transactionRepository = new FakeTransactionRepository();
        var financialAccountRepository = new FakeFinancialAccountRepository(account);
        var categoryRepository = new FakeTransactionCategoryRepository(category);
        var service = CreateService(transactionRepository, financialAccountRepository, categoryRepository, nowUtc);

        var result = await service.RegisterIncomeAsync(
            new CreateIncomeTransactionInput(
                userId,
                account.Id,
                category.Id,
                75m,
                new DateOnly(2026, 4, 8),
                "  Bonus  "),
            CancellationToken.None);

        Assert.Equal(TransactionType.Income, result.Type);
        Assert.Equal(175m, account.CurrentBalanceSnapshot);
        Assert.Single(transactionRepository.AddedTransactions);
        Assert.Equal(account.Id, result.FinancialAccountId);
        Assert.Equal(category.Id, result.TransactionCategoryId);
        Assert.Equal("Bonus", transactionRepository.AddedTransactions[0].Description);
        Assert.Equal(1, transactionRepository.SaveChangesCalls);
    }

    [Fact]
    public async Task RegisterExpenseAsync_ShouldRejectCategoryWithDifferentType()
    {
        var nowUtc = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var account = FinancialAccount.Create(
            userId,
            "Carteira",
            FinancialAccountType.Wallet,
            80m,
            null,
            null,
            nowUtc.AddDays(-5));
        var incompatibleCategory = TransactionCategory.CreateUserCategory(
            userId,
            "Salario",
            TransactionCategoryType.Income,
            null,
            null,
            nowUtc.AddDays(-4));

        var service = CreateService(
            new FakeTransactionRepository(),
            new FakeFinancialAccountRepository(account),
            new FakeTransactionCategoryRepository(incompatibleCategory),
            nowUtc);

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => service.RegisterExpenseAsync(
            new CreateExpenseTransactionInput(
                userId,
                account.Id,
                incompatibleCategory.Id,
                20m,
                new DateOnly(2026, 4, 8),
                "Mercado"),
            CancellationToken.None));

        Assert.Equal("A categoria informada nao e compativel com o tipo da transacao.", exception.Message);
        Assert.Equal(80m, account.CurrentBalanceSnapshot);
    }

    [Fact]
    public async Task RegisterTransferAsync_ShouldMoveBalanceBetweenAccountsAndPersistTransfer()
    {
        var nowUtc = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var sourceAccount = FinancialAccount.Create(
            userId,
            "Conta origem",
            FinancialAccountType.BankAccount,
            200m,
            null,
            null,
            nowUtc.AddDays(-10));
        var destinationAccount = FinancialAccount.Create(
            userId,
            "Conta destino",
            FinancialAccountType.InvestmentAccount,
            50m,
            null,
            null,
            nowUtc.AddDays(-9));

        var accountRepository = new FakeFinancialAccountRepository(sourceAccount, destinationAccount);
        var transactionRepository = new FakeTransactionRepository();
        var service = CreateService(
            transactionRepository,
            accountRepository,
            new FakeTransactionCategoryRepository(),
            nowUtc);

        var result = await service.RegisterTransferAsync(
            new CreateTransferTransactionInput(
                userId,
                sourceAccount.Id,
                destinationAccount.Id,
                60m,
                new DateOnly(2026, 4, 8),
                "Reserva"),
            CancellationToken.None);

        Assert.Equal(TransactionType.Transfer, result.Type);
        Assert.Equal(140m, sourceAccount.CurrentBalanceSnapshot);
        Assert.Equal(110m, destinationAccount.CurrentBalanceSnapshot);
        Assert.Single(transactionRepository.AddedTransactions);
        Assert.Equal(sourceAccount.Id, result.SourceFinancialAccountId);
        Assert.Equal(destinationAccount.Id, result.DestinationFinancialAccountId);
    }

    [Fact]
    public async Task GetByPeriodAsync_ShouldRejectInvalidDateRange()
    {
        var service = CreateService(
            new FakeTransactionRepository(),
            new FakeFinancialAccountRepository(),
            new FakeTransactionCategoryRepository(),
            new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc));

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => service.GetByPeriodAsync(
            new GetTransactionsByPeriodInput(
                Guid.NewGuid(),
                new DateOnly(2026, 4, 10),
                new DateOnly(2026, 4, 8),
                null,
                null),
            CancellationToken.None));

        Assert.Equal("A data inicial nao pode ser maior que a data final.", exception.Message);
    }

    private static TransactionService CreateService(
        FakeTransactionRepository transactionRepository,
        FakeFinancialAccountRepository financialAccountRepository,
        FakeTransactionCategoryRepository categoryRepository,
        DateTime nowUtc)
    {
        return new TransactionService(
            transactionRepository,
            financialAccountRepository,
            categoryRepository,
            new FakeDateTimeProvider(nowUtc));
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        public List<Transaction> AddedTransactions { get; } = [];
        public IReadOnlyList<Transaction> TransactionsToReturn { get; set; } = [];
        public int SaveChangesCalls { get; private set; }

        public Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
        {
            AddedTransactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Transaction>> GetByUserAndPeriodAsync(
            Guid userId,
            DateOnly from,
            DateOnly to,
            TransactionType? type,
            Guid? financialAccountId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(TransactionsToReturn);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFinancialAccountRepository : IFinancialAccountRepository
    {
        private readonly Dictionary<Guid, FinancialAccount> _accounts;

        public FakeFinancialAccountRepository(params FinancialAccount[] accounts)
        {
            _accounts = accounts.ToDictionary(x => x.Id);
        }

        public Task AddAsync(FinancialAccount financialAccount, CancellationToken cancellationToken)
        {
            _accounts[financialAccount.Id] = financialAccount;
            return Task.CompletedTask;
        }

        public Task<FinancialAccount?> GetByUserIdAndIdAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken)
        {
            _accounts.TryGetValue(financialAccountId, out var account);
            return Task.FromResult(account is not null && account.UserId == userId ? account : null);
        }

        public Task<IReadOnlyList<FinancialAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            IReadOnlyList<FinancialAccount> accounts = _accounts.Values.Where(x => x.UserId == userId).ToList();
            return Task.FromResult(accounts);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTransactionCategoryRepository : ITransactionCategoryRepository
    {
        private readonly Dictionary<Guid, TransactionCategory> _categories;

        public FakeTransactionCategoryRepository(params TransactionCategory[] categories)
        {
            _categories = categories.ToDictionary(x => x.Id);
        }

        public Task AddAsync(TransactionCategory transactionCategory, CancellationToken cancellationToken)
        {
            _categories[transactionCategory.Id] = transactionCategory;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByUserAndNameAndTypeAsync(Guid userId, string normalizedName, TransactionCategoryType type, CancellationToken cancellationToken)
        {
            var exists = _categories.Values.Any(x =>
                x.UserId == userId &&
                string.Equals(x.Name, normalizedName, StringComparison.OrdinalIgnoreCase) &&
                x.Type == type);
            return Task.FromResult(exists);
        }

        public Task<TransactionCategory?> GetByUserIdAndIdAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken)
        {
            _categories.TryGetValue(transactionCategoryId, out var category);
            return Task.FromResult(category is not null && category.UserId == userId ? category : null);
        }

        public Task<IReadOnlyList<TransactionCategory>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            IReadOnlyList<TransactionCategory> categories = _categories.Values.Where(x => x.UserId == userId).ToList();
            return Task.FromResult(categories);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

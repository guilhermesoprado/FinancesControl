using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.ScheduledEntries.Contracts;
using FinanceManager.Application.ScheduledEntries.Services;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Tests;

public sealed class ScheduledEntryServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistRecurringEntry()
    {
        var nowUtc = new DateTime(2026, 4, 18, 14, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var financialAccount = FinancialAccount.Create(userId, "Conta principal", FinancialAccountType.BankAccount, 1000m, null, null, nowUtc.AddDays(-5));
        var category = TransactionCategory.CreateUserCategory(userId, "Aluguel", TransactionCategoryType.Expense, null, null, nowUtc.AddDays(-4));
        var repository = new FakeScheduledEntryRepository();
        var service = CreateService(repository, nowUtc, financialAccount, category);

        var result = await service.CreateAsync(
            new CreateScheduledEntryInput(
                userId,
                financialAccount.Id,
                category.Id,
                ScheduledEntryPlanningMode.Recurring,
                ScheduledEntryRecurrenceFrequency.Monthly,
                950m,
                "Apartamento",
                new DateOnly(2026, 5, 5),
                null),
            CancellationToken.None);

        Assert.Equal("Conta principal", result.FinancialAccountName);
        Assert.Equal("Aluguel", result.TransactionCategoryName);
        Assert.Equal(TransactionType.Expense, result.Type);
        Assert.Single(repository.AddedEntries);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectCategoryThatDoesNotBelongToUser()
    {
        var nowUtc = new DateTime(2026, 4, 18, 14, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var financialAccount = FinancialAccount.Create(userId, "Conta principal", FinancialAccountType.BankAccount, 1000m, null, null, nowUtc.AddDays(-5));
        var anotherUserCategory = TransactionCategory.CreateUserCategory(Guid.NewGuid(), "Outra categoria", TransactionCategoryType.Expense, null, null, nowUtc.AddDays(-4));
        var service = CreateService(new FakeScheduledEntryRepository(), nowUtc, financialAccount, anotherUserCategory);

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(
            new CreateScheduledEntryInput(
                userId,
                financialAccount.Id,
                anotherUserCategory.Id,
                ScheduledEntryPlanningMode.OneTime,
                null,
                10m,
                null,
                new DateOnly(2026, 5, 5),
                null),
            CancellationToken.None));

        Assert.Equal("A categoria informada nao foi encontrada para o usuario autenticado.", exception.Message);
    }

    [Fact]
    public async Task GetByUserAsync_ShouldReturnMappedEntriesFilteredByStatus()
    {
        var nowUtc = new DateTime(2026, 4, 18, 14, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var financialAccount = FinancialAccount.Create(userId, "Conta principal", FinancialAccountType.BankAccount, 1000m, null, null, nowUtc.AddDays(-5));
        var category = TransactionCategory.CreateUserCategory(userId, "Salario", TransactionCategoryType.Income, null, null, nowUtc.AddDays(-4));
        var scheduled = ScheduledEntry.Create(userId, financialAccount.Id, category.Id, TransactionType.Income, ScheduledEntryPlanningMode.OneTime, null, 100m, "Bonus", new DateOnly(2026, 5, 1), null, nowUtc.AddDays(-2));

        var repository = new FakeScheduledEntryRepository(scheduled);
        var service = CreateService(repository, nowUtc, financialAccount, category);

        var result = await service.GetByUserAsync(
            new GetScheduledEntriesInput(userId, ScheduledEntryStatus.Scheduled, null, null),
            CancellationToken.None);

        var single = Assert.Single(result);
        Assert.Equal(scheduled.Id, single.ScheduledEntryId);
        Assert.Equal("Conta principal", single.FinancialAccountName);
        Assert.Equal(new DateOnly(2026, 5, 1), single.OccurrenceDate);
    }

    [Fact]
    public async Task GetByUserAsync_ShouldReturnCompletedOccurrenceAndNextRecurringOccurrence()
    {
        var nowUtc = new DateTime(2026, 4, 18, 14, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var financialAccount = FinancialAccount.Create(userId, "Conta principal", FinancialAccountType.BankAccount, 1000m, null, null, nowUtc.AddDays(-5));
        var category = TransactionCategory.CreateUserCategory(userId, "Internet", TransactionCategoryType.Expense, null, null, nowUtc.AddDays(-4));
        var recurring = ScheduledEntry.Create(userId, financialAccount.Id, category.Id, TransactionType.Expense, ScheduledEntryPlanningMode.Recurring, ScheduledEntryRecurrenceFrequency.Monthly, 120m, "Internet", new DateOnly(2026, 4, 10), null, nowUtc.AddDays(-30));
        recurring.MarkAsCompleted(nowUtc);

        var occurrenceRepository = new FakeScheduledEntryOccurrenceRepository();
        await occurrenceRepository.AddAsync(
            ScheduledEntryOccurrence.Create(userId, recurring.Id, new DateOnly(2026, 4, 10), ScheduledEntryStatus.Completed, nowUtc),
            CancellationToken.None);

        var service = new ScheduledEntryService(
            new FakeScheduledEntryRepository(recurring),
            occurrenceRepository,
            new FakeFinancialAccountRepository(financialAccount),
            new FakeTransactionCategoryRepository(category),
            new FakeDateTimeProvider(nowUtc));

        var result = await service.GetByUserAsync(
            new GetScheduledEntriesInput(userId, null, new DateOnly(2026, 4, 1), new DateOnly(2026, 5, 31)),
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Status == ScheduledEntryStatus.Completed && x.OccurrenceDate == new DateOnly(2026, 4, 10));
        Assert.Contains(result, x => x.Status == ScheduledEntryStatus.Scheduled && x.OccurrenceDate == new DateOnly(2026, 5, 10));
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistEditedScheduledEntry()
    {
        var nowUtc = new DateTime(2026, 4, 18, 14, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var accountA = FinancialAccount.Create(userId, "Conta A", FinancialAccountType.BankAccount, 1000m, null, null, nowUtc.AddDays(-5));
        var accountB = FinancialAccount.Create(userId, "Conta B", FinancialAccountType.BankAccount, 500m, null, null, nowUtc.AddDays(-5));
        var categoryA = TransactionCategory.CreateUserCategory(userId, "Internet", TransactionCategoryType.Expense, null, null, nowUtc.AddDays(-4));
        var categoryB = TransactionCategory.CreateUserCategory(userId, "Salario", TransactionCategoryType.Income, null, null, nowUtc.AddDays(-4));
        var entry = ScheduledEntry.Create(userId, accountA.Id, categoryA.Id, TransactionType.Expense, ScheduledEntryPlanningMode.Recurring, ScheduledEntryRecurrenceFrequency.Monthly, 120m, "Internet", new DateOnly(2026, 4, 20), null, nowUtc.AddDays(-2));
        var repository = new FakeScheduledEntryRepository(entry);
        var service = new ScheduledEntryService(
            repository,
            new FakeScheduledEntryOccurrenceRepository(),
            new FakeFinancialAccountRepository(accountA, accountB),
            new FakeTransactionCategoryRepository(categoryA, categoryB),
            new FakeDateTimeProvider(nowUtc));

        entry.MarkAsCompleted(nowUtc.AddDays(-1));

        var result = await service.UpdateAsync(
            new UpdateScheduledEntryInput(
                userId,
                entry.Id,
                accountB.Id,
                categoryB.Id,
                ScheduledEntryPlanningMode.Recurring,
                ScheduledEntryRecurrenceFrequency.Monthly,
                300m,
                "Salario projetado",
                new DateOnly(2026, 4, 20),
                new DateOnly(2026, 12, 20)),
            CancellationToken.None);

        Assert.Equal(accountB.Id, result.FinancialAccountId);
        Assert.Equal("Conta B", result.FinancialAccountName);
        Assert.Equal(categoryB.Id, result.TransactionCategoryId);
        Assert.Equal("Salario", result.TransactionCategoryName);
        Assert.Equal(TransactionType.Income, result.Type);
        Assert.Equal(ScheduledEntryPlanningMode.Recurring, result.PlanningMode);
        Assert.Equal(ScheduledEntryRecurrenceFrequency.Monthly, result.RecurrenceFrequency);
        Assert.Equal(300m, result.Amount);
        Assert.Equal(new DateOnly(2026, 4, 20), result.StartDate);
        Assert.Equal(new DateOnly(2026, 5, 20), result.NextOccurrenceDate);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task CompleteAsync_ShouldAdvanceRecurringEntry()
    {
        var nowUtc = new DateTime(2026, 4, 18, 14, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var financialAccount = FinancialAccount.Create(userId, "Conta principal", FinancialAccountType.BankAccount, 1000m, null, null, nowUtc.AddDays(-5));
        var category = TransactionCategory.CreateUserCategory(userId, "Internet", TransactionCategoryType.Expense, null, null, nowUtc.AddDays(-4));
        var entry = ScheduledEntry.Create(userId, financialAccount.Id, category.Id, TransactionType.Expense, ScheduledEntryPlanningMode.Recurring, ScheduledEntryRecurrenceFrequency.Monthly, 120m, "Internet", new DateOnly(2026, 5, 10), null, nowUtc.AddDays(-2));
        var repository = new FakeScheduledEntryRepository(entry);
        var occurrenceRepository = new FakeScheduledEntryOccurrenceRepository();
        var service = new ScheduledEntryService(
            repository,
            occurrenceRepository,
            new FakeFinancialAccountRepository(financialAccount),
            new FakeTransactionCategoryRepository(category),
            new FakeDateTimeProvider(nowUtc));

        var result = await service.CompleteAsync(
            new ApplyScheduledEntryOccurrenceActionInput(userId, entry.Id, new DateOnly(2026, 5, 10)),
            CancellationToken.None);

        Assert.Equal(ScheduledEntryStatus.Scheduled, result.Status);
        Assert.Equal(new DateOnly(2026, 6, 10), result.NextOccurrenceDate);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Single(occurrenceRepository.AddedOccurrences);
        Assert.Equal(new DateOnly(2026, 5, 10), occurrenceRepository.AddedOccurrences[0].OccurrenceDate);
        Assert.Equal(ScheduledEntryStatus.Completed, occurrenceRepository.AddedOccurrences[0].Status);
    }

    [Fact]
    public async Task CompleteAsync_ShouldRejectWhenRequestedOccurrenceIsNotCurrent()
    {
        var nowUtc = new DateTime(2026, 4, 18, 14, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var financialAccount = FinancialAccount.Create(userId, "Conta principal", FinancialAccountType.BankAccount, 1000m, null, null, nowUtc.AddDays(-5));
        var category = TransactionCategory.CreateUserCategory(userId, "Internet", TransactionCategoryType.Expense, null, null, nowUtc.AddDays(-4));
        var entry = ScheduledEntry.Create(userId, financialAccount.Id, category.Id, TransactionType.Expense, ScheduledEntryPlanningMode.Recurring, ScheduledEntryRecurrenceFrequency.Monthly, 120m, "Internet", new DateOnly(2026, 5, 10), null, nowUtc.AddDays(-2));
        var service = new ScheduledEntryService(
            new FakeScheduledEntryRepository(entry),
            new FakeScheduledEntryOccurrenceRepository(),
            new FakeFinancialAccountRepository(financialAccount),
            new FakeTransactionCategoryRepository(category),
            new FakeDateTimeProvider(nowUtc));

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => service.CompleteAsync(
            new ApplyScheduledEntryOccurrenceActionInput(userId, entry.Id, new DateOnly(2026, 5, 11)),
            CancellationToken.None));

        Assert.Equal("A recorrencia foi alterada e a competencia selecionada nao esta mais ativa. Recarregue a lista antes de continuar.", exception.Message);
    }

    [Fact]
    public async Task CancelAsync_ShouldSetCancelledStatus()
    {
        var nowUtc = new DateTime(2026, 4, 18, 14, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var financialAccount = FinancialAccount.Create(userId, "Conta principal", FinancialAccountType.BankAccount, 1000m, null, null, nowUtc.AddDays(-5));
        var category = TransactionCategory.CreateUserCategory(userId, "Internet", TransactionCategoryType.Expense, null, null, nowUtc.AddDays(-4));
        var entry = ScheduledEntry.Create(userId, financialAccount.Id, category.Id, TransactionType.Expense, ScheduledEntryPlanningMode.OneTime, null, 120m, "Internet", new DateOnly(2026, 5, 10), null, nowUtc.AddDays(-2));
        var repository = new FakeScheduledEntryRepository(entry);
        var service = CreateService(repository, nowUtc, financialAccount, category);

        var result = await service.CancelAsync(
            new ApplyScheduledEntryOccurrenceActionInput(userId, entry.Id, new DateOnly(2026, 5, 10)),
            CancellationToken.None);

        Assert.Equal(ScheduledEntryStatus.Cancelled, result.Status);
        Assert.Null(result.NextOccurrenceDate);
    }

    private static ScheduledEntryService CreateService(
        FakeScheduledEntryRepository scheduledEntryRepository,
        DateTime nowUtc,
        FinancialAccount financialAccount,
        TransactionCategory category)
    {
        return new ScheduledEntryService(
            scheduledEntryRepository,
            new FakeScheduledEntryOccurrenceRepository(),
            new FakeFinancialAccountRepository(financialAccount),
            new FakeTransactionCategoryRepository(category),
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

    private sealed class FakeScheduledEntryRepository : IScheduledEntryRepository
    {
        private readonly Dictionary<Guid, ScheduledEntry> _entries;

        public FakeScheduledEntryRepository(params ScheduledEntry[] entries)
        {
            _entries = entries.ToDictionary(x => x.Id);
            AddedEntries = entries.ToList();
        }

        public List<ScheduledEntry> AddedEntries { get; }
        public int SaveChangesCalls { get; private set; }

        public Task AddAsync(ScheduledEntry scheduledEntry, CancellationToken cancellationToken)
        {
            _entries[scheduledEntry.Id] = scheduledEntry;
            if (AddedEntries.All(existing => existing.Id != scheduledEntry.Id))
            {
                AddedEntries.Add(scheduledEntry);
            }

            return Task.CompletedTask;
        }

        public Task<bool> ExistsActiveByUserAndFinancialAccountIdAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_entries.Values.Any(x =>
                x.UserId == userId
                && x.FinancialAccountId == financialAccountId
                && x.Status == ScheduledEntryStatus.Scheduled));
        }

        public Task<bool> ExistsActiveByUserAndTransactionCategoryIdAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_entries.Values.Any(x =>
                x.UserId == userId
                && x.TransactionCategoryId == transactionCategoryId
                && x.Status == ScheduledEntryStatus.Scheduled));
        }

        public Task<ScheduledEntry?> GetByUserAndIdAsync(Guid userId, Guid scheduledEntryId, CancellationToken cancellationToken)
        {
            _entries.TryGetValue(scheduledEntryId, out var entry);
            return Task.FromResult(entry is not null && entry.UserId == userId ? entry : null);
        }

        public Task<IReadOnlyList<ScheduledEntry>> GetByUserAsync(Guid userId, ScheduledEntryStatus? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
        {
            IEnumerable<ScheduledEntry> query = _entries.Values.Where(x => x.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            if (!status.HasValue || status == ScheduledEntryStatus.Scheduled)
            {
                if (from.HasValue)
                {
                    query = query.Where(x => (x.NextOccurrenceDate ?? x.StartDate) >= from.Value);
                }

                if (to.HasValue)
                {
                    query = query.Where(x => (x.NextOccurrenceDate ?? x.StartDate) <= to.Value);
                }
            }
            else if (status == ScheduledEntryStatus.Completed)
            {
                if (from.HasValue)
                {
                    var fromUtc = new DateTime(from.Value.Year, from.Value.Month, from.Value.Day, 0, 0, 0, DateTimeKind.Utc);
                    query = query.Where(x => (x.LastRealizedAtUtc ?? x.UpdatedAtUtc) >= fromUtc);
                }

                if (to.HasValue)
                {
                    var nextDay = to.Value.AddDays(1);
                    var toExclusiveUtc = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, 0, 0, 0, DateTimeKind.Utc);
                    query = query.Where(x => (x.LastRealizedAtUtc ?? x.UpdatedAtUtc) < toExclusiveUtc);
                }
            }
            else
            {
                if (from.HasValue)
                {
                    var fromUtc = new DateTime(from.Value.Year, from.Value.Month, from.Value.Day, 0, 0, 0, DateTimeKind.Utc);
                    query = query.Where(x => x.UpdatedAtUtc >= fromUtc);
                }

                if (to.HasValue)
                {
                    var nextDay = to.Value.AddDays(1);
                    var toExclusiveUtc = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, 0, 0, 0, DateTimeKind.Utc);
                    query = query.Where(x => x.UpdatedAtUtc < toExclusiveUtc);
                }
            }

            return Task.FromResult((IReadOnlyList<ScheduledEntry>)query.ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeScheduledEntryOccurrenceRepository : IScheduledEntryOccurrenceRepository
    {
        private readonly List<ScheduledEntryOccurrence> _occurrences = [];

        public List<ScheduledEntryOccurrence> AddedOccurrences => _occurrences;

        public Task AddAsync(ScheduledEntryOccurrence occurrence, CancellationToken cancellationToken)
        {
            _occurrences.Add(occurrence);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ScheduledEntryOccurrence>> GetByUserAsync(Guid userId, ScheduledEntryStatus? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
        {
            IEnumerable<ScheduledEntryOccurrence> query = _occurrences.Where(x => x.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            if (from.HasValue)
            {
                query = query.Where(x => x.OccurrenceDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.OccurrenceDate <= to.Value);
            }

            return Task.FromResult((IReadOnlyList<ScheduledEntryOccurrence>)query.ToList());
        }
    }

    private sealed class FakeFinancialAccountRepository : IFinancialAccountRepository
    {
        private readonly Dictionary<Guid, FinancialAccount> _financialAccounts;

        public FakeFinancialAccountRepository(params FinancialAccount[] financialAccounts)
        {
            _financialAccounts = financialAccounts.ToDictionary(x => x.Id);
        }

        public Task AddAsync(FinancialAccount financialAccount, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<FinancialAccount?> GetByUserIdAndIdAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken)
        {
            _financialAccounts.TryGetValue(financialAccountId, out var account);
            return Task.FromResult(account is not null && account.UserId == userId ? account : null);
        }

        public Task<IReadOnlyList<FinancialAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult((IReadOnlyList<FinancialAccount>)_financialAccounts.Values.Where(x => x.UserId == userId).ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTransactionCategoryRepository : ITransactionCategoryRepository
    {
        private readonly Dictionary<Guid, TransactionCategory> _categories;

        public FakeTransactionCategoryRepository(params TransactionCategory[] categories)
        {
            _categories = categories.ToDictionary(x => x.Id);
        }

        public Task AddAsync(TransactionCategory transactionCategory, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> ExistsByUserAndNameAndTypeAsync(Guid userId, string normalizedName, TransactionCategoryType type, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<TransactionCategory?> GetByUserIdAndIdAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken)
        {
            _categories.TryGetValue(transactionCategoryId, out var category);
            return Task.FromResult(category is not null && category.UserId == userId ? category : null);
        }

        public Task<IReadOnlyList<TransactionCategory>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult((IReadOnlyList<TransactionCategory>)_categories.Values.Where(x => x.UserId == userId).ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

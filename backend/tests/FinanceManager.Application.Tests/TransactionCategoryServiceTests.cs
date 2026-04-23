using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.TransactionCategories.Contracts;
using FinanceManager.Application.TransactionCategories.Services;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.Tests;

public sealed class TransactionCategoryServiceTests
{
    [Fact]
    public async Task UpdateAsync_ShouldPersistControlledChanges()
    {
        var nowUtc = new DateTime(2026, 4, 22, 13, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var category = TransactionCategory.CreateUserCategory(
            userId,
            "Mercado",
            TransactionCategoryType.Expense,
            "#22c55e",
            "cart",
            nowUtc.AddDays(-3));
        var repository = new FakeTransactionCategoryRepository(category);
        var auditRepository = new FakeAuditLogRepository();
        var scheduledEntryRepository = new FakeScheduledEntryRepository();
        var service = new TransactionCategoryService(auditRepository, scheduledEntryRepository, repository, new FakeDateTimeProvider(nowUtc));

        var result = await service.UpdateAsync(
            new UpdateTransactionCategoryInput(
                userId,
                category.Id,
                "Compras",
                "#ff9900",
                "bag"),
            CancellationToken.None);

        Assert.Equal("Compras", result.Name);
        Assert.Equal("#ff9900", result.Color);
        Assert.Equal("bag", result.Icon);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Single(auditRepository.Logs);
        Assert.Equal(AuditLogAction.Updated, auditRepository.Logs[0].Action);
        Assert.Equal(AuditLogEntityType.TransactionCategory, auditRepository.Logs[0].EntityType);
    }

    [Fact]
    public async Task InactivateAsync_ShouldInactivateUserCategory()
    {
        var nowUtc = new DateTime(2026, 4, 22, 13, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var category = TransactionCategory.CreateUserCategory(
            userId,
            "Lazer",
            TransactionCategoryType.Expense,
            null,
            null,
            nowUtc.AddDays(-2));
        var repository = new FakeTransactionCategoryRepository(category);
        var auditRepository = new FakeAuditLogRepository();
        var scheduledEntryRepository = new FakeScheduledEntryRepository();
        var service = new TransactionCategoryService(auditRepository, scheduledEntryRepository, repository, new FakeDateTimeProvider(nowUtc));

        var result = await service.InactivateAsync(
            new InactivateTransactionCategoryInput(userId, category.Id),
            CancellationToken.None);

        Assert.False(result.IsActive);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Single(auditRepository.Logs);
        Assert.Equal(AuditLogAction.Inactivated, auditRepository.Logs[0].Action);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectDuplicatedNameForSameType()
    {
        var nowUtc = new DateTime(2026, 4, 22, 13, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var categoryA = TransactionCategory.CreateUserCategory(
            userId,
            "Mercado",
            TransactionCategoryType.Expense,
            null,
            null,
            nowUtc.AddDays(-4));
        var categoryB = TransactionCategory.CreateUserCategory(
            userId,
            "Farmacia",
            TransactionCategoryType.Expense,
            null,
            null,
            nowUtc.AddDays(-3));
        var repository = new FakeTransactionCategoryRepository(categoryA, categoryB);
        var auditRepository = new FakeAuditLogRepository();
        var scheduledEntryRepository = new FakeScheduledEntryRepository();
        var service = new TransactionCategoryService(auditRepository, scheduledEntryRepository, repository, new FakeDateTimeProvider(nowUtc));

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(
            new UpdateTransactionCategoryInput(
                userId,
                categoryB.Id,
                "Mercado",
                null,
                null),
            CancellationToken.None));

        Assert.Equal("Ja existe uma categoria com o mesmo nome e tipo para este usuario.", exception.Message);
        Assert.Empty(auditRepository.Logs);
    }

    [Fact]
    public async Task InactivateAsync_ShouldRejectCategoryLinkedToActiveScheduledEntries()
    {
        var nowUtc = new DateTime(2026, 4, 22, 13, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var category = TransactionCategory.CreateUserCategory(
            userId,
            "Moradia",
            TransactionCategoryType.Expense,
            null,
            null,
            nowUtc.AddDays(-2));
        var repository = new FakeTransactionCategoryRepository(category);
        var auditRepository = new FakeAuditLogRepository();
        var scheduledEntryRepository = new FakeScheduledEntryRepository { HasActiveTransactionCategoryReference = true };
        var service = new TransactionCategoryService(auditRepository, scheduledEntryRepository, repository, new FakeDateTimeProvider(nowUtc));

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => service.InactivateAsync(
            new InactivateTransactionCategoryInput(userId, category.Id),
            CancellationToken.None));

        Assert.Equal("Nao e possivel inativar uma categoria vinculada a lancamentos planejados ativos.", exception.Message);
        Assert.Empty(auditRepository.Logs);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class FakeTransactionCategoryRepository : ITransactionCategoryRepository
    {
        private readonly Dictionary<Guid, TransactionCategory> _categories;

        public FakeTransactionCategoryRepository(params TransactionCategory[] categories)
        {
            _categories = categories.ToDictionary(x => x.Id);
        }

        public int SaveChangesCalls { get; private set; }

        public Task AddAsync(TransactionCategory transactionCategory, CancellationToken cancellationToken)
        {
            _categories[transactionCategory.Id] = transactionCategory;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByUserAndNameAndTypeAsync(Guid userId, string normalizedName, TransactionCategoryType type, CancellationToken cancellationToken)
        {
            var exists = _categories.Values.Any(x =>
                x.UserId == userId
                && x.Type == type
                && string.Equals(x.Name.Trim().ToUpperInvariant(), normalizedName, StringComparison.Ordinal));

            return Task.FromResult(exists);
        }

        public Task<TransactionCategory?> GetByUserIdAndIdAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken)
        {
            _categories.TryGetValue(transactionCategoryId, out var category);
            return Task.FromResult(category is not null && category.UserId == userId ? category : null);
        }

        public Task<IReadOnlyList<TransactionCategory>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult((IReadOnlyList<TransactionCategory>)_categories.Values.Where(x => x.UserId == userId).ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public List<AuditLog> Logs { get; } = [];

        public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
        {
            Logs.Add(auditLog);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditLog>> GetByUserAsync(
            Guid userId,
            AuditLogEntityType? entityType,
            AuditLogAction? action,
            Guid? entityId,
            string? search,
            DateTime? fromUtc,
            DateTime? toUtc,
            int limit,
            CancellationToken cancellationToken)
        {
            return Task.FromResult((IReadOnlyList<AuditLog>)Logs.ToList());
        }
    }

    private sealed class FakeScheduledEntryRepository : IScheduledEntryRepository
    {
        public bool HasActiveTransactionCategoryReference { get; set; }

        public Task AddAsync(ScheduledEntry scheduledEntry, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> ExistsActiveByUserAndFinancialAccountIdAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<bool> ExistsActiveByUserAndTransactionCategoryIdAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken)
            => Task.FromResult(HasActiveTransactionCategoryReference);

        public Task<ScheduledEntry?> GetByUserAndIdAsync(Guid userId, Guid scheduledEntryId, CancellationToken cancellationToken)
            => Task.FromResult<ScheduledEntry?>(null);

        public Task<IReadOnlyList<ScheduledEntry>> GetByUserAsync(Guid userId, ScheduledEntryStatus? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyList<ScheduledEntry>)[]);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

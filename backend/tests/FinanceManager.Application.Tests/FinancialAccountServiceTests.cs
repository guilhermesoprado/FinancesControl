using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.FinancialAccounts.Contracts;
using FinanceManager.Application.FinancialAccounts.Services;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.Tests;

public sealed class FinancialAccountServiceTests
{
    [Fact]
    public async Task UpdateAsync_ShouldPersistControlledChanges()
    {
        var nowUtc = new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var account = FinancialAccount.Create(
            userId,
            "Conta antiga",
            FinancialAccountType.BankAccount,
            0m,
            "Banco A",
            "Descricao antiga",
            nowUtc.AddDays(-3));
        var repository = new FakeFinancialAccountRepository(account);
        var auditRepository = new FakeAuditLogRepository();
        var scheduledEntryRepository = new FakeScheduledEntryRepository();
        var service = new FinancialAccountService(auditRepository, repository, scheduledEntryRepository, new FakeDateTimeProvider(nowUtc));

        var result = await service.UpdateAsync(
            new UpdateFinancialAccountInput(
                userId,
                account.Id,
                "Conta principal",
                FinancialAccountType.Wallet,
                "Banco B",
                "Descricao nova"),
            CancellationToken.None);

        Assert.Equal("Conta principal", result.Name);
        Assert.Equal(FinancialAccountType.Wallet, result.Type);
        Assert.Equal("Banco B", result.InstitutionName);
        Assert.Equal("Descricao nova", result.Description);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Single(auditRepository.Logs);
        Assert.Equal(AuditLogAction.Updated, auditRepository.Logs[0].Action);
        Assert.Equal(AuditLogEntityType.FinancialAccount, auditRepository.Logs[0].EntityType);
    }

    [Fact]
    public async Task InactivateAsync_ShouldRejectAccountWithVisibleBalance()
    {
        var nowUtc = new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var account = FinancialAccount.Create(
            userId,
            "Conta saldo",
            FinancialAccountType.BankAccount,
            100m,
            null,
            null,
            nowUtc.AddDays(-2));
        var repository = new FakeFinancialAccountRepository(account);
        var auditRepository = new FakeAuditLogRepository();
        var scheduledEntryRepository = new FakeScheduledEntryRepository();
        var service = new FinancialAccountService(auditRepository, repository, scheduledEntryRepository, new FakeDateTimeProvider(nowUtc));

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => service.InactivateAsync(
            new InactivateFinancialAccountInput(userId, account.Id),
            CancellationToken.None));

        Assert.Equal("Nao e possivel inativar uma conta financeira com saldo visivel diferente de zero.", exception.Message);
        Assert.Empty(auditRepository.Logs);
    }

    [Fact]
    public async Task InactivateAsync_ShouldInactivateAccountWhenBalanceIsZero()
    {
        var nowUtc = new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var account = FinancialAccount.Create(
            userId,
            "Conta zerada",
            FinancialAccountType.BankAccount,
            0m,
            null,
            null,
            nowUtc.AddDays(-2));
        var repository = new FakeFinancialAccountRepository(account);
        var auditRepository = new FakeAuditLogRepository();
        var scheduledEntryRepository = new FakeScheduledEntryRepository();
        var service = new FinancialAccountService(auditRepository, repository, scheduledEntryRepository, new FakeDateTimeProvider(nowUtc));

        var result = await service.InactivateAsync(
            new InactivateFinancialAccountInput(userId, account.Id),
            CancellationToken.None);

        Assert.False(result.IsActive);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Single(auditRepository.Logs);
        Assert.Equal(AuditLogAction.Inactivated, auditRepository.Logs[0].Action);
    }

    [Fact]
    public async Task InactivateAsync_ShouldRejectAccountLinkedToActiveScheduledEntries()
    {
        var nowUtc = new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var account = FinancialAccount.Create(
            userId,
            "Conta planejada",
            FinancialAccountType.BankAccount,
            0m,
            null,
            null,
            nowUtc.AddDays(-2));
        var repository = new FakeFinancialAccountRepository(account);
        var auditRepository = new FakeAuditLogRepository();
        var scheduledEntryRepository = new FakeScheduledEntryRepository { HasActiveFinancialAccountReference = true };
        var service = new FinancialAccountService(auditRepository, repository, scheduledEntryRepository, new FakeDateTimeProvider(nowUtc));

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => service.InactivateAsync(
            new InactivateFinancialAccountInput(userId, account.Id),
            CancellationToken.None));

        Assert.Equal("Nao e possivel inativar uma conta financeira vinculada a lancamentos planejados ativos.", exception.Message);
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

    private sealed class FakeFinancialAccountRepository : IFinancialAccountRepository
    {
        private readonly Dictionary<Guid, FinancialAccount> _accounts;

        public FakeFinancialAccountRepository(params FinancialAccount[] accounts)
        {
            _accounts = accounts.ToDictionary(x => x.Id);
        }

        public int SaveChangesCalls { get; private set; }

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
            return Task.FromResult((IReadOnlyList<FinancialAccount>)_accounts.Values.Where(x => x.UserId == userId).ToList());
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
        public bool HasActiveFinancialAccountReference { get; set; }

        public Task AddAsync(ScheduledEntry scheduledEntry, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> ExistsActiveByUserAndFinancialAccountIdAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken)
            => Task.FromResult(HasActiveFinancialAccountReference);

        public Task<bool> ExistsActiveByUserAndTransactionCategoryIdAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<ScheduledEntry?> GetByUserAndIdAsync(Guid userId, Guid scheduledEntryId, CancellationToken cancellationToken)
            => Task.FromResult<ScheduledEntry?>(null);

        public Task<IReadOnlyList<ScheduledEntry>> GetByUserAsync(Guid userId, ScheduledEntryStatus? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyList<ScheduledEntry>)[]);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

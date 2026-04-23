using FinanceManager.Application.AuditLogs.Contracts;
using FinanceManager.Application.AuditLogs.Services;
using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Tests;

public sealed class AuditLogServiceTests
{
    [Fact]
    public async Task GetByUserAsync_ShouldReturnFilteredAuditLogs()
    {
        var userId = Guid.NewGuid();
        var logs = new[]
        {
            AuditLog.Create(userId, AuditLogEntityType.FinancialAccount, Guid.NewGuid(), AuditLogAction.Created, "Conta criada", new DateTime(2026, 4, 23, 12, 0, 0, DateTimeKind.Utc)),
            AuditLog.Create(userId, AuditLogEntityType.TransactionCategory, Guid.NewGuid(), AuditLogAction.Updated, "Categoria atualizada", new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc)),
        };
        var service = new AuditLogService(new FakeAuditLogRepository(logs));

        var result = await service.GetByUserAsync(
            new GetAuditLogsInput(userId, AuditLogEntityType.FinancialAccount, null, null, null, null, null, 50),
            CancellationToken.None);

        var single = Assert.Single(result);
        Assert.Equal(AuditLogEntityType.FinancialAccount, single.EntityType);
        Assert.Equal(AuditLogAction.Created, single.Action);
    }

    [Fact]
    public async Task GetByUserAsync_ShouldRejectInvalidDateRange()
    {
        var userId = Guid.NewGuid();
        var service = new AuditLogService(new FakeAuditLogRepository());

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => service.GetByUserAsync(
            new GetAuditLogsInput(userId, null, null, null, null, new DateOnly(2026, 4, 25), new DateOnly(2026, 4, 20), 100),
            CancellationToken.None));

        Assert.Equal("A data inicial da auditoria nao pode ser maior que a data final.", exception.Message);
    }

    [Fact]
    public async Task GetByUserAsync_ShouldFilterByEntityIdAndSearch()
    {
        var userId = Guid.NewGuid();
        var trackedEntityId = Guid.NewGuid();
        var logs = new[]
        {
            AuditLog.Create(userId, AuditLogEntityType.FinancialAccount, trackedEntityId, AuditLogAction.Updated, "Conta principal atualizada", new DateTime(2026, 4, 23, 12, 0, 0, DateTimeKind.Utc)),
            AuditLog.Create(userId, AuditLogEntityType.FinancialAccount, Guid.NewGuid(), AuditLogAction.Updated, "Conta secundaria atualizada", new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc)),
        };
        var service = new AuditLogService(new FakeAuditLogRepository(logs));

        var result = await service.GetByUserAsync(
            new GetAuditLogsInput(userId, null, AuditLogAction.Updated, trackedEntityId, "principal", null, null, 100),
            CancellationToken.None);

        var single = Assert.Single(result);
        Assert.Equal(trackedEntityId, single.EntityId);
        Assert.Contains("principal", single.Summary, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        private readonly IReadOnlyList<AuditLog> _logs;

        public FakeAuditLogRepository(params AuditLog[] logs)
        {
            _logs = logs;
        }

        public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken) => Task.CompletedTask;

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
            IEnumerable<AuditLog> query = _logs.Where(x => x.UserId == userId);

            if (entityType.HasValue)
            {
                query = query.Where(x => x.EntityType == entityType.Value);
            }

            if (action.HasValue)
            {
                query = query.Where(x => x.Action == action.Value);
            }

            if (entityId.HasValue)
            {
                query = query.Where(x => x.EntityId == entityId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.Summary.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (fromUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc < toUtc.Value);
            }

            return Task.FromResult((IReadOnlyList<AuditLog>)query.OrderByDescending(x => x.CreatedAtUtc).Take(limit).ToList());
        }
    }
}

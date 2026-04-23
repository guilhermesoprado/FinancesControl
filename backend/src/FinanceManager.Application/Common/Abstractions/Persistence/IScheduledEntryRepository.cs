using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Common.Abstractions.Persistence;

public interface IScheduledEntryRepository
{
    Task AddAsync(ScheduledEntry scheduledEntry, CancellationToken cancellationToken);
    Task<bool> ExistsActiveByUserAndFinancialAccountIdAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken);
    Task<bool> ExistsActiveByUserAndTransactionCategoryIdAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken);
    Task<ScheduledEntry?> GetByUserAndIdAsync(Guid userId, Guid scheduledEntryId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ScheduledEntry>> GetByUserAsync(
        Guid userId,
        ScheduledEntryStatus? status,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

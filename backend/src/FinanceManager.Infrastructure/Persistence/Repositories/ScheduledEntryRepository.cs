using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class ScheduledEntryRepository : IScheduledEntryRepository
{
    private readonly FinanceManagerDbContext _dbContext;

    public ScheduledEntryRepository(FinanceManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(ScheduledEntry scheduledEntry, CancellationToken cancellationToken)
    {
        return _dbContext.ScheduledEntries.AddAsync(scheduledEntry, cancellationToken).AsTask();
    }

    public Task<bool> ExistsActiveByUserAndFinancialAccountIdAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken)
    {
        return _dbContext.ScheduledEntries.AnyAsync(
            x => x.UserId == userId
                && x.FinancialAccountId == financialAccountId
                && x.Status == ScheduledEntryStatus.Scheduled,
            cancellationToken);
    }

    public Task<bool> ExistsActiveByUserAndTransactionCategoryIdAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken)
    {
        return _dbContext.ScheduledEntries.AnyAsync(
            x => x.UserId == userId
                && x.TransactionCategoryId == transactionCategoryId
                && x.Status == ScheduledEntryStatus.Scheduled,
            cancellationToken);
    }

    public Task<ScheduledEntry?> GetByUserAndIdAsync(Guid userId, Guid scheduledEntryId, CancellationToken cancellationToken)
    {
        return _dbContext.ScheduledEntries
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == scheduledEntryId, cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledEntry>> GetByUserAsync(
        Guid userId,
        ScheduledEntryStatus? status,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ScheduledEntries
            .Where(x => x.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!status.HasValue || status == ScheduledEntryStatus.Scheduled)
        {
            if (from.HasValue)
            {
                var fromDate = from.Value;
                query = query.Where(x => (x.NextOccurrenceDate ?? x.StartDate) >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value;
                query = query.Where(x => (x.NextOccurrenceDate ?? x.StartDate) <= toDate);
            }
        }
        else if (status == ScheduledEntryStatus.Completed)
        {
            if (from.HasValue)
            {
                var fromUtc = CreateDayStartUtc(from.Value);
                query = query.Where(x => (x.LastRealizedAtUtc ?? x.UpdatedAtUtc) >= fromUtc);
            }

            if (to.HasValue)
            {
                var toExclusiveUtc = CreateDayEndExclusiveUtc(to.Value);
                query = query.Where(x => (x.LastRealizedAtUtc ?? x.UpdatedAtUtc) < toExclusiveUtc);
            }
        }
        else
        {
            if (from.HasValue)
            {
                var fromUtc = CreateDayStartUtc(from.Value);
                query = query.Where(x => x.UpdatedAtUtc >= fromUtc);
            }

            if (to.HasValue)
            {
                var toExclusiveUtc = CreateDayEndExclusiveUtc(to.Value);
                query = query.Where(x => x.UpdatedAtUtc < toExclusiveUtc);
            }
        }

        return await query
            .OrderBy(x => x.NextOccurrenceDate ?? x.StartDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DateTime CreateDayStartUtc(DateOnly date)
    {
        return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private static DateTime CreateDayEndExclusiveUtc(DateOnly date)
    {
        var nextDay = date.AddDays(1);
        return new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, 0, 0, 0, DateTimeKind.Utc);
    }
}

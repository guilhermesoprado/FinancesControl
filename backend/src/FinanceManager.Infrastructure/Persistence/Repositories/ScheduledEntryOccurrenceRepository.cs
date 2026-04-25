using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class ScheduledEntryOccurrenceRepository : IScheduledEntryOccurrenceRepository
{
    private readonly FinanceManagerDbContext _dbContext;

    public ScheduledEntryOccurrenceRepository(FinanceManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(ScheduledEntryOccurrence occurrence, CancellationToken cancellationToken)
    {
        return _dbContext.ScheduledEntryOccurrences.AddAsync(occurrence, cancellationToken).AsTask();
    }

    public Task<ScheduledEntryOccurrence?> GetByUserScheduledEntryAndDateAsync(
        Guid userId,
        Guid scheduledEntryId,
        DateOnly occurrenceDate,
        CancellationToken cancellationToken)
    {
        return _dbContext.ScheduledEntryOccurrences.FirstOrDefaultAsync(
            x => x.UserId == userId
                && x.ScheduledEntryId == scheduledEntryId
                && x.OccurrenceDate == occurrenceDate,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledEntryOccurrence>> GetByUserAsync(
        Guid userId,
        ScheduledEntryStatus? status,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ScheduledEntryOccurrences
            .Where(x => x.UserId == userId);

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

        return await query
            .OrderBy(x => x.OccurrenceDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public void Remove(ScheduledEntryOccurrence occurrence)
    {
        _dbContext.ScheduledEntryOccurrences.Remove(occurrence);
    }
}

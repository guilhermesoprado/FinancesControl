using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Common.Abstractions.Persistence;

public interface IScheduledEntryOccurrenceRepository
{
    Task AddAsync(ScheduledEntryOccurrence occurrence, CancellationToken cancellationToken);
    Task<ScheduledEntryOccurrence?> GetByUserScheduledEntryAndDateAsync(
        Guid userId,
        Guid scheduledEntryId,
        DateOnly occurrenceDate,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ScheduledEntryOccurrence>> GetByUserAsync(
        Guid userId,
        ScheduledEntryStatus? status,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken);
    void Remove(ScheduledEntryOccurrence occurrence);
}

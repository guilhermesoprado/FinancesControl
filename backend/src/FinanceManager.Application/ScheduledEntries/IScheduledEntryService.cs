using FinanceManager.Application.ScheduledEntries.Contracts;

namespace FinanceManager.Application.ScheduledEntries;

public interface IScheduledEntryService
{
    Task<ScheduledEntryDto> CreateAsync(CreateScheduledEntryInput input, CancellationToken cancellationToken);
    Task<ScheduledEntryDto> UpdateAsync(UpdateScheduledEntryInput input, CancellationToken cancellationToken);
    Task<IReadOnlyList<ScheduledEntryOccurrenceDto>> GetByUserAsync(GetScheduledEntriesInput input, CancellationToken cancellationToken);
    Task<ScheduledEntryDto> CompleteAsync(ApplyScheduledEntryOccurrenceActionInput input, CancellationToken cancellationToken);
    Task<ScheduledEntryDto> UndoCompleteAsync(ApplyScheduledEntryOccurrenceActionInput input, CancellationToken cancellationToken);
    Task<ScheduledEntryDto> SkipAsync(ApplyScheduledEntryOccurrenceActionInput input, CancellationToken cancellationToken);
    Task<ScheduledEntryDto> CancelAsync(ApplyScheduledEntryOccurrenceActionInput input, CancellationToken cancellationToken);
}

namespace FinanceManager.Application.ScheduledEntries.Contracts;

public sealed record ApplyScheduledEntryOccurrenceActionInput(
    Guid UserId,
    Guid ScheduledEntryId,
    DateOnly OccurrenceDate);

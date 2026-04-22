namespace FinanceManager.Api.Contracts.Requests.ScheduledEntries;

public sealed record ScheduledEntryOccurrenceActionRequest(
    DateOnly OccurrenceDate);

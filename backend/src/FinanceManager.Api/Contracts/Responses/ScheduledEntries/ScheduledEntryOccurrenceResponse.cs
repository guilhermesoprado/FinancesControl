namespace FinanceManager.Api.Contracts.Responses.ScheduledEntries;

public sealed record ScheduledEntryOccurrenceResponse(
    string OccurrenceKey,
    Guid ScheduledEntryId,
    Guid FinancialAccountId,
    string FinancialAccountName,
    Guid TransactionCategoryId,
    string TransactionCategoryName,
    string Type,
    string PlanningMode,
    string? RecurrenceFrequency,
    decimal Amount,
    string? Description,
    DateOnly StartDate,
    DateOnly OccurrenceDate,
    DateOnly? NextOccurrenceDate,
    DateOnly? EndDate,
    string Status,
    DateTime? TreatedAtUtc,
    bool CanEdit,
    bool CanAct,
    DateTime CreatedAtUtc);

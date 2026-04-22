namespace FinanceManager.Api.Contracts.Requests.ScheduledEntries;

public sealed record UpdateScheduledEntryRequest(
    Guid FinancialAccountId,
    Guid TransactionCategoryId,
    string PlanningMode,
    string? RecurrenceFrequency,
    decimal Amount,
    string? Description,
    DateOnly StartDate,
    DateOnly? EndDate);

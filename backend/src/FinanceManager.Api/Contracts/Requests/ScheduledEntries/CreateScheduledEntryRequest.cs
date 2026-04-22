namespace FinanceManager.Api.Contracts.Requests.ScheduledEntries;

public sealed record CreateScheduledEntryRequest(
    Guid FinancialAccountId,
    Guid TransactionCategoryId,
    string PlanningMode,
    string? RecurrenceFrequency,
    decimal Amount,
    string? Description,
    DateOnly StartDate,
    DateOnly? EndDate);

namespace FinanceManager.Api.Contracts.Responses.ScheduledEntries;

public sealed record ScheduledEntryResponse(
    Guid Id,
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
    DateOnly? NextOccurrenceDate,
    DateOnly? EndDate,
    string Status,
    DateTime? LastRealizedAtUtc,
    DateTime CreatedAtUtc);

using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.ScheduledEntries.Contracts;

public sealed record ScheduledEntryDto(
    Guid Id,
    Guid FinancialAccountId,
    string FinancialAccountName,
    Guid TransactionCategoryId,
    string TransactionCategoryName,
    TransactionType Type,
    ScheduledEntryPlanningMode PlanningMode,
    ScheduledEntryRecurrenceFrequency? RecurrenceFrequency,
    decimal Amount,
    string? Description,
    DateOnly StartDate,
    DateOnly? NextOccurrenceDate,
    DateOnly? EndDate,
    ScheduledEntryStatus Status,
    DateTime? LastRealizedAtUtc,
    DateTime CreatedAtUtc);

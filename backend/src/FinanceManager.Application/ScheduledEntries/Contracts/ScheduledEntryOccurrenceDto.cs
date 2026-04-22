using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.ScheduledEntries.Contracts;

public sealed record ScheduledEntryOccurrenceDto(
    string OccurrenceKey,
    Guid ScheduledEntryId,
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
    DateOnly OccurrenceDate,
    DateOnly? NextOccurrenceDate,
    DateOnly? EndDate,
    ScheduledEntryStatus Status,
    DateTime? TreatedAtUtc,
    bool CanEdit,
    bool CanAct,
    DateTime CreatedAtUtc);

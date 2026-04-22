using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.ScheduledEntries.Contracts;

public sealed record UpdateScheduledEntryInput(
    Guid UserId,
    Guid ScheduledEntryId,
    Guid FinancialAccountId,
    Guid TransactionCategoryId,
    ScheduledEntryPlanningMode PlanningMode,
    ScheduledEntryRecurrenceFrequency? RecurrenceFrequency,
    decimal Amount,
    string? Description,
    DateOnly StartDate,
    DateOnly? EndDate);

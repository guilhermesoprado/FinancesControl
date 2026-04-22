using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.ScheduledEntries.Contracts;

public sealed record GetScheduledEntriesInput(
    Guid UserId,
    ScheduledEntryStatus? Status,
    DateOnly? From,
    DateOnly? To);

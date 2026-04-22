using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Tests;

public sealed class ScheduledEntryDomainTests
{
    [Fact]
    public void Create_ShouldPopulateOneTimeEntryWithTrimmedDescription()
    {
        var nowUtc = new DateTime(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc);
        var entry = ScheduledEntry.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Expense,
            ScheduledEntryPlanningMode.OneTime,
            null,
            150m,
            "  Aluguel  ",
            new DateOnly(2026, 5, 5),
            null,
            nowUtc);

        Assert.Equal(ScheduledEntryStatus.Scheduled, entry.Status);
        Assert.Equal("Aluguel", entry.Description);
        Assert.Equal(new DateOnly(2026, 5, 5), entry.NextOccurrenceDate);
        Assert.Equal(nowUtc, entry.CreatedAtUtc);
        Assert.Equal(nowUtc, entry.UpdatedAtUtc);
    }

    [Fact]
    public void MarkAsCompleted_ShouldAdvanceRecurringEntryToNextOccurrence()
    {
        var nowUtc = new DateTime(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc);
        var entry = ScheduledEntry.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Expense,
            ScheduledEntryPlanningMode.Recurring,
            ScheduledEntryRecurrenceFrequency.Monthly,
            220m,
            "Academia",
            new DateOnly(2026, 5, 10),
            new DateOnly(2026, 8, 10),
            nowUtc);

        entry.MarkAsCompleted(nowUtc.AddDays(1));

        Assert.Equal(ScheduledEntryStatus.Scheduled, entry.Status);
        Assert.Equal(new DateOnly(2026, 6, 10), entry.NextOccurrenceDate);
        Assert.Equal(nowUtc.AddDays(1), entry.LastRealizedAtUtc);
    }

    [Fact]
    public void Skip_ShouldFinalizeRecurringEntryWhenEndDateIsReached()
    {
        var nowUtc = new DateTime(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc);
        var entry = ScheduledEntry.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Income,
            ScheduledEntryPlanningMode.Recurring,
            ScheduledEntryRecurrenceFrequency.Weekly,
            80m,
            "Freela",
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 1),
            nowUtc);

        entry.Skip(nowUtc.AddDays(2));

        Assert.Equal(ScheduledEntryStatus.Skipped, entry.Status);
        Assert.Null(entry.NextOccurrenceDate);
    }

    [Fact]
    public void Create_ShouldRejectRecurringEntryWithoutFrequency()
    {
        var action = () => ScheduledEntry.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Expense,
            ScheduledEntryPlanningMode.Recurring,
            null,
            50m,
            null,
            new DateOnly(2026, 5, 1),
            null,
            DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("Lancamentos planejados recorrentes exigem frequencia recorrente.", exception.Message);
    }

    [Fact]
    public void Update_ShouldRefreshPlannedFieldsForScheduledEntry()
    {
        var nowUtc = new DateTime(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc);
        var entry = ScheduledEntry.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Expense,
            ScheduledEntryPlanningMode.OneTime,
            null,
            120m,
            "Internet",
            new DateOnly(2026, 5, 10),
            null,
            nowUtc);

        var updatedAt = nowUtc.AddHours(2);
        var newAccountId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();

        entry.Update(
            newAccountId,
            newCategoryId,
            TransactionType.Income,
            ScheduledEntryPlanningMode.Recurring,
            ScheduledEntryRecurrenceFrequency.Weekly,
            250m,
            "  Freelance semanal  ",
            new DateOnly(2026, 5, 14),
            new DateOnly(2026, 7, 14),
            updatedAt);

        Assert.Equal(newAccountId, entry.FinancialAccountId);
        Assert.Equal(newCategoryId, entry.TransactionCategoryId);
        Assert.Equal(TransactionType.Income, entry.Type);
        Assert.Equal(ScheduledEntryPlanningMode.Recurring, entry.PlanningMode);
        Assert.Equal(ScheduledEntryRecurrenceFrequency.Weekly, entry.RecurrenceFrequency);
        Assert.Equal(250m, entry.Amount);
        Assert.Equal("Freelance semanal", entry.Description);
        Assert.Equal(new DateOnly(2026, 5, 14), entry.StartDate);
        Assert.Equal(new DateOnly(2026, 5, 14), entry.NextOccurrenceDate);
        Assert.Equal(new DateOnly(2026, 7, 14), entry.EndDate);
        Assert.Equal(updatedAt, entry.UpdatedAtUtc);
    }
}

using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Persistence.Context;
using FinanceManager.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Tests;

public sealed class ScheduledEntryRepositoryTests
{
    [Fact]
    public async Task GetByUserAsync_ShouldFilterByStatusAndDateRange()
    {
        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc);

        await using var dbContext = CreateDbContext();
        var repository = new ScheduledEntryRepository(dbContext);

        var scheduled = ScheduledEntry.Create(userId, accountId, categoryId, TransactionType.Expense, ScheduledEntryPlanningMode.OneTime, null, 80m, "Conta 1", new DateOnly(2026, 5, 5), null, nowUtc);
        var completed = ScheduledEntry.Create(userId, accountId, categoryId, TransactionType.Expense, ScheduledEntryPlanningMode.OneTime, null, 120m, "Conta 2", new DateOnly(2026, 4, 20), null, nowUtc.AddMinutes(-10));
        completed.MarkAsCompleted(nowUtc.AddMinutes(-5));
        var anotherUser = ScheduledEntry.Create(anotherUserId, accountId, categoryId, TransactionType.Expense, ScheduledEntryPlanningMode.OneTime, null, 999m, "Outro", new DateOnly(2026, 5, 5), null, nowUtc);

        await repository.AddAsync(scheduled, CancellationToken.None);
        await repository.AddAsync(completed, CancellationToken.None);
        await repository.AddAsync(anotherUser, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var filtered = await repository.GetByUserAsync(
            userId,
            ScheduledEntryStatus.Scheduled,
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 31),
            CancellationToken.None);

        var single = Assert.Single(filtered);
        Assert.Equal(scheduled.Id, single.Id);
    }

    [Fact]
    public async Task GetByUserAsync_ShouldFilterCompletedEntriesByCompletionDate()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc);

        await using var dbContext = CreateDbContext();
        var repository = new ScheduledEntryRepository(dbContext);

        var completed = ScheduledEntry.Create(userId, accountId, categoryId, TransactionType.Expense, ScheduledEntryPlanningMode.OneTime, null, 120m, "Conta concluida", new DateOnly(2026, 4, 1), null, nowUtc.AddDays(-10));
        completed.MarkAsCompleted(nowUtc);

        await repository.AddAsync(completed, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var filtered = await repository.GetByUserAsync(
            userId,
            ScheduledEntryStatus.Completed,
            new DateOnly(2026, 4, 18),
            new DateOnly(2026, 4, 18),
            CancellationToken.None);

        var single = Assert.Single(filtered);
        Assert.Equal(completed.Id, single.Id);
    }

    [Fact]
    public async Task GetByUserAsync_ShouldFilterCancelledEntriesByUpdatedDate()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc);

        await using var dbContext = CreateDbContext();
        var repository = new ScheduledEntryRepository(dbContext);

        var cancelled = ScheduledEntry.Create(userId, accountId, categoryId, TransactionType.Expense, ScheduledEntryPlanningMode.OneTime, null, 90m, "Conta cancelada", new DateOnly(2026, 4, 1), null, nowUtc.AddDays(-10));
        cancelled.Cancel(nowUtc);

        await repository.AddAsync(cancelled, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var filtered = await repository.GetByUserAsync(
            userId,
            ScheduledEntryStatus.Cancelled,
            new DateOnly(2026, 4, 18),
            new DateOnly(2026, 4, 18),
            CancellationToken.None);

        var single = Assert.Single(filtered);
        Assert.Equal(cancelled.Id, single.Id);
    }

    private static FinanceManagerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceManagerDbContext>()
            .UseInMemoryDatabase($"scheduled-entries-tests-{Guid.NewGuid()}")
            .Options;

        return new FinanceManagerDbContext(options);
    }
}

using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Persistence.Context;
using FinanceManager.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Tests;

public sealed class InvoiceRepositoryTests
{
    [Fact]
    public async Task GetByUserAsync_ShouldReturnInvoicesFilteredAndOrdered()
    {
        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var creditCardId = Guid.NewGuid();
        var otherCardId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 4, 9, 16, 0, 0, DateTimeKind.Utc);

        await using var dbContext = CreateDbContext();
        var repository = new InvoiceRepository(dbContext);

        await repository.AddAsync(Invoice.Open(userId, creditCardId, 2026, 3, new DateOnly(2026, 2, 11), new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 18), nowUtc), CancellationToken.None);
        await repository.AddAsync(Invoice.Open(userId, creditCardId, 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), nowUtc.AddMinutes(1)), CancellationToken.None);
        await repository.AddAsync(Invoice.Open(userId, otherCardId, 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), nowUtc.AddMinutes(2)), CancellationToken.None);
        await repository.AddAsync(Invoice.Open(anotherUserId, creditCardId, 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), nowUtc.AddMinutes(3)), CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var all = await repository.GetByUserAsync(userId, null, CancellationToken.None);
        Assert.Equal(3, all.Count);
        Assert.Equal(4, all[0].ReferenceMonth);

        var filtered = await repository.GetByUserAsync(userId, creditCardId, CancellationToken.None);
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, invoice => Assert.Equal(creditCardId, invoice.CreditCardId));
    }

    private static FinanceManagerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceManagerDbContext>()
            .UseInMemoryDatabase($"invoices-tests-{Guid.NewGuid()}")
            .Options;

        return new FinanceManagerDbContext(options);
    }
}
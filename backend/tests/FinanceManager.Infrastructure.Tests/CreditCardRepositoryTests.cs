using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Persistence.Context;
using FinanceManager.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Tests;

public sealed class CreditCardRepositoryTests
{
    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnOnlyCreditCardsFromAuthenticatedUserOrderedByName()
    {
        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc);

        await using var dbContext = CreateDbContext();
        var repository = new CreditCardRepository(dbContext);

        await repository.AddAsync(CreditCard.Create(userId, "Zeta", null, 1000m, 7, 14, null, nowUtc), CancellationToken.None);
        await repository.AddAsync(CreditCard.Create(userId, "Alpha", null, 1500m, 8, 15, null, nowUtc), CancellationToken.None);
        await repository.AddAsync(CreditCard.Create(anotherUserId, "Outro", null, 999m, 5, 10, null, nowUtc), CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var result = await repository.GetByUserIdAsync(userId, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Zeta", result[1].Name);
    }

    private static FinanceManagerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceManagerDbContext>()
            .UseInMemoryDatabase($"credit-cards-tests-{Guid.NewGuid()}")
            .Options;

        return new FinanceManagerDbContext(options);
    }
}
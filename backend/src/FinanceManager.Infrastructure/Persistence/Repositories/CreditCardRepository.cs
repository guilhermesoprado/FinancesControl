using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class CreditCardRepository : ICreditCardRepository
{
    private readonly FinanceManagerDbContext _dbContext;

    public CreditCardRepository(FinanceManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(CreditCard creditCard, CancellationToken cancellationToken)
    {
        return _dbContext.CreditCards.AddAsync(creditCard, cancellationToken).AsTask();
    }

    public Task<CreditCard?> GetByUserIdAndIdAsync(Guid userId, Guid creditCardId, CancellationToken cancellationToken)
    {
        return _dbContext.CreditCards.FirstOrDefaultAsync(
            x => x.UserId == userId && x.Id == creditCardId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<CreditCard>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.CreditCards
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
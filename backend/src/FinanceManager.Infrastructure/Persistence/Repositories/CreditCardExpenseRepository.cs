using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class CreditCardExpenseRepository : ICreditCardExpenseRepository
{
    private readonly FinanceManagerDbContext _dbContext;

    public CreditCardExpenseRepository(FinanceManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(CreditCardExpense expense, CancellationToken cancellationToken)
    {
        return _dbContext.CreditCardExpenses.AddAsync(expense, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<CreditCardExpense>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.CreditCardExpenses
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.OccurredOn)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

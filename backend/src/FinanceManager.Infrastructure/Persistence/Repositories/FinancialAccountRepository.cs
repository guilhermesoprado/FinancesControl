using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class FinancialAccountRepository : IFinancialAccountRepository
{
    private readonly FinanceManagerDbContext _dbContext;

    public FinancialAccountRepository(FinanceManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(FinancialAccount financialAccount, CancellationToken cancellationToken)
    {
        return _dbContext.FinancialAccounts.AddAsync(financialAccount, cancellationToken).AsTask();
    }

    public Task<FinancialAccount?> GetByUserIdAndIdAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken)
    {
        return _dbContext.FinancialAccounts.FirstOrDefaultAsync(
            x => x.UserId == userId && x.Id == financialAccountId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.FinancialAccounts
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

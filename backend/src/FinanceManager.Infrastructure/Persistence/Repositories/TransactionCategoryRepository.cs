using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class TransactionCategoryRepository : ITransactionCategoryRepository
{
    private readonly FinanceManagerDbContext _dbContext;

    public TransactionCategoryRepository(FinanceManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(TransactionCategory transactionCategory, CancellationToken cancellationToken)
    {
        return _dbContext.TransactionCategories.AddAsync(transactionCategory, cancellationToken).AsTask();
    }

    public Task<bool> ExistsByUserAndNameAndTypeAsync(
        Guid userId,
        string normalizedName,
        TransactionCategoryType type,
        CancellationToken cancellationToken)
    {
        return _dbContext.TransactionCategories.AnyAsync(
            x => x.UserId == userId
                && !x.IsSystem
                && x.Type == type
                && x.Name.ToUpper() == normalizedName,
            cancellationToken);
    }

    public async Task<IReadOnlyList<TransactionCategory>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.TransactionCategories
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

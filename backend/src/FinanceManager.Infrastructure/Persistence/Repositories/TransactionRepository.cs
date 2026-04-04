using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly FinanceManagerDbContext _dbContext;

    public TransactionRepository(FinanceManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        return _dbContext.Transactions.AddAsync(transaction, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<Transaction>> GetByUserAndPeriodAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        TransactionType? type,
        Guid? financialAccountId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Transactions
            .Where(x => x.UserId == userId && x.OccurredOn >= from && x.OccurredOn <= to);

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        if (financialAccountId.HasValue)
        {
            var accountId = financialAccountId.Value;
            query = query.Where(x =>
                x.FinancialAccountId == accountId ||
                x.SourceFinancialAccountId == accountId ||
                x.DestinationFinancialAccountId == accountId);
        }

        return await query
            .OrderByDescending(x => x.OccurredOn)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
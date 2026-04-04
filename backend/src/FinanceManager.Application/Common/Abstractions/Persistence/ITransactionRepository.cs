using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Common.Abstractions.Persistence;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken);
    Task<IReadOnlyList<Transaction>> GetByUserAndPeriodAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        TransactionType? type,
        Guid? financialAccountId,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

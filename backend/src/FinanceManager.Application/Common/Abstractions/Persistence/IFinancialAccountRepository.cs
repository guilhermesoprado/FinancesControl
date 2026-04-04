using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.Common.Abstractions.Persistence;

public interface IFinancialAccountRepository
{
    Task AddAsync(FinancialAccount financialAccount, CancellationToken cancellationToken);
    Task<IReadOnlyList<FinancialAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

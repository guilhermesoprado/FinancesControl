using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.Common.Abstractions.Persistence;

public interface ICreditCardExpenseRepository
{
    Task AddAsync(CreditCardExpense expense, CancellationToken cancellationToken);
    Task<IReadOnlyList<CreditCardExpense>> GetByUserAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

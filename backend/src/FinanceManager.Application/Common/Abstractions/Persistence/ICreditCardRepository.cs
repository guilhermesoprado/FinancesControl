using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.Common.Abstractions.Persistence;

public interface ICreditCardRepository
{
    Task AddAsync(CreditCard creditCard, CancellationToken cancellationToken);
    Task<CreditCard?> GetByUserIdAndIdAsync(Guid userId, Guid creditCardId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CreditCard>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
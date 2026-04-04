using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.Common.Abstractions.Persistence;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

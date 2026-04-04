using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly FinanceManagerDbContext _dbContext;

    public UserRepository(FinanceManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return _dbContext.Users.AnyAsync(x => x.EmailNormalized == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return _dbContext.Users.SingleOrDefaultAsync(x => x.EmailNormalized == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        return _dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

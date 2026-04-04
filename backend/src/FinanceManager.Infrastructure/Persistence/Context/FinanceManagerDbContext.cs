using FinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Context;

public sealed class FinanceManagerDbContext : DbContext
{
    public FinanceManagerDbContext(DbContextOptions<FinanceManagerDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
    public DbSet<TransactionCategory> TransactionCategories => Set<TransactionCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanceManagerDbContext).Assembly);
    }
}

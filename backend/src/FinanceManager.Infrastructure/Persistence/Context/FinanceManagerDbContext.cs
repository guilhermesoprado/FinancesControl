using FinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Context;

public sealed class FinanceManagerDbContext : DbContext
{
    public FinanceManagerDbContext(DbContextOptions<FinanceManagerDbContext> options) : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<CreditCardExpense> CreditCardExpenses => Set<CreditCardExpense>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<ScheduledEntry> ScheduledEntries => Set<ScheduledEntry>();
    public DbSet<ScheduledEntryOccurrence> ScheduledEntryOccurrences => Set<ScheduledEntryOccurrence>();
    public DbSet<TransactionCategory> TransactionCategories => Set<TransactionCategory>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanceManagerDbContext).Assembly);
    }
}

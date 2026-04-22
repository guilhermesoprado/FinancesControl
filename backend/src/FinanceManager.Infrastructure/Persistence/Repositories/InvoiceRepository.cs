using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly FinanceManagerDbContext _dbContext;

    public InvoiceRepository(FinanceManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        return _dbContext.Invoices.AddAsync(invoice, cancellationToken).AsTask();
    }

    public Task<bool> ExistsByUserCreditCardAndReferenceAsync(Guid userId, Guid creditCardId, int referenceYear, int referenceMonth, CancellationToken cancellationToken)
    {
        return _dbContext.Invoices.AnyAsync(
            x => x.UserId == userId &&
                 x.CreditCardId == creditCardId &&
                 x.ReferenceYear == referenceYear &&
                 x.ReferenceMonth == referenceMonth,
            cancellationToken);
    }

    public Task<Invoice?> GetByUserAndIdAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken)
    {
        return _dbContext.Invoices.FirstOrDefaultAsync(
            x => x.UserId == userId && x.Id == invoiceId,
            cancellationToken);
    }

    public Task<Invoice?> GetByUserCreditCardAndReferenceAsync(Guid userId, Guid creditCardId, int referenceYear, int referenceMonth, CancellationToken cancellationToken)
    {
        return _dbContext.Invoices.FirstOrDefaultAsync(
            x => x.UserId == userId &&
                 x.CreditCardId == creditCardId &&
                 x.ReferenceYear == referenceYear &&
                 x.ReferenceMonth == referenceMonth,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetByUserAsync(Guid userId, Guid? creditCardId, CancellationToken cancellationToken)
    {
        var query = _dbContext.Invoices.Where(x => x.UserId == userId);

        if (creditCardId.HasValue)
        {
            query = query.Where(x => x.CreditCardId == creditCardId.Value);
        }

        return await query
            .OrderByDescending(x => x.ReferenceYear)
            .ThenByDescending(x => x.ReferenceMonth)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

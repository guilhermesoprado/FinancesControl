using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.Common.Abstractions.Persistence;

public interface IInvoiceRepository
{
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken);
    Task<bool> ExistsByUserCreditCardAndReferenceAsync(Guid userId, Guid creditCardId, int referenceYear, int referenceMonth, CancellationToken cancellationToken);
    Task<Invoice?> GetByUserAndIdAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken);
    Task<Invoice?> GetByUserCreditCardAndReferenceAsync(Guid userId, Guid creditCardId, int referenceYear, int referenceMonth, CancellationToken cancellationToken);
    Task<IReadOnlyList<Invoice>> GetByUserAsync(Guid userId, Guid? creditCardId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

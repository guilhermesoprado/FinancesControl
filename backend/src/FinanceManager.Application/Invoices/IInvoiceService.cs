using FinanceManager.Application.Invoices.Contracts;

namespace FinanceManager.Application.Invoices;

public interface IInvoiceService
{
    Task<InvoiceDto> CreateAsync(CreateInvoiceInput input, CancellationToken cancellationToken);
    Task<InvoiceDto> CloseAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<InvoiceDto>> GetByUserAsync(Guid userId, Guid? creditCardId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CreditCardExpenseDto>> GetCardExpensesByUserAsync(Guid userId, Guid? creditCardId, Guid? invoiceId, CancellationToken cancellationToken);
    Task<InvoiceDto> PayAsync(PayInvoiceInput input, CancellationToken cancellationToken);
    Task<InvoiceDto> AdjustAsync(AdjustInvoiceInput input, CancellationToken cancellationToken);
    Task<InvoiceDto> RegisterCardExpenseAsync(RegisterCardExpenseInput input, CancellationToken cancellationToken);
}

namespace FinanceManager.Application.Invoices.Contracts;

public sealed record CreateInvoiceInput(
    Guid UserId,
    Guid CreditCardId,
    int ReferenceYear,
    int ReferenceMonth);
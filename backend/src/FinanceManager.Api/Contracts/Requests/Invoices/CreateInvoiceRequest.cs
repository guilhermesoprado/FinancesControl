namespace FinanceManager.Api.Contracts.Requests.Invoices;

public sealed record CreateInvoiceRequest(
    Guid CreditCardId,
    int ReferenceYear,
    int ReferenceMonth);
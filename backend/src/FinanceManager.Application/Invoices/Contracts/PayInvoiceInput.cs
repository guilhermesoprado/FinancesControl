namespace FinanceManager.Application.Invoices.Contracts;

public sealed record PayInvoiceInput(
    Guid UserId,
    Guid InvoiceId,
    Guid FinancialAccountId,
    decimal Amount);

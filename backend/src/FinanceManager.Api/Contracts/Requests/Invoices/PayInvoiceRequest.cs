namespace FinanceManager.Api.Contracts.Requests.Invoices;

public sealed record PayInvoiceRequest(
    Guid FinancialAccountId,
    decimal Amount);

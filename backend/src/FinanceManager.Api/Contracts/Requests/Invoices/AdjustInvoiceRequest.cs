namespace FinanceManager.Api.Contracts.Requests.Invoices;

public sealed record AdjustInvoiceRequest(
    string AdjustmentType,
    decimal Amount);

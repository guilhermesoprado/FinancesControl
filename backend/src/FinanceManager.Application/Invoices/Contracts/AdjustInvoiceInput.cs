using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Invoices.Contracts;

public sealed record AdjustInvoiceInput(
    Guid UserId,
    Guid InvoiceId,
    InvoiceAdjustmentType AdjustmentType,
    decimal Amount);

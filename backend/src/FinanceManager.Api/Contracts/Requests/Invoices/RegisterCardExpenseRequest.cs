namespace FinanceManager.Api.Contracts.Requests.Invoices;

public sealed record RegisterCardExpenseRequest(
    Guid CreditCardId,
    Guid TransactionCategoryId,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description,
    Guid? TargetInvoiceId = null,
    int InstallmentCount = 1);

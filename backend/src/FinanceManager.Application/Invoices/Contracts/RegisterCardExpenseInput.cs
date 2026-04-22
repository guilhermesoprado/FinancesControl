namespace FinanceManager.Application.Invoices.Contracts;

public sealed record RegisterCardExpenseInput(
    Guid UserId,
    Guid CreditCardId,
    Guid TransactionCategoryId,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description,
    Guid? TargetInvoiceId = null,
    int InstallmentCount = 1);

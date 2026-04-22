namespace FinanceManager.Application.Invoices.Contracts;

public sealed record CreditCardExpenseDto(
    Guid Id,
    Guid CreditCardId,
    Guid InvoiceId,
    Guid TransactionCategoryId,
    string TransactionCategoryName,
    Guid InstallmentGroupId,
    int InstallmentNumber,
    int InstallmentCount,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description,
    DateTime CreatedAtUtc);

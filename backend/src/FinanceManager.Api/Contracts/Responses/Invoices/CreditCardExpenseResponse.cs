namespace FinanceManager.Api.Contracts.Responses.Invoices;

public sealed record CreditCardExpenseResponse(
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

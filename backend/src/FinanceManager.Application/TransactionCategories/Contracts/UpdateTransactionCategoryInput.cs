namespace FinanceManager.Application.TransactionCategories.Contracts;

public sealed record UpdateTransactionCategoryInput(
    Guid UserId,
    Guid TransactionCategoryId,
    string Name,
    string? Color,
    string? Icon);

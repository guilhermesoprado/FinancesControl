namespace FinanceManager.Application.TransactionCategories.Contracts;

public sealed record InactivateTransactionCategoryInput(
    Guid UserId,
    Guid TransactionCategoryId);

namespace FinanceManager.Api.Contracts.Requests.TransactionCategories;

public sealed record UpdateTransactionCategoryRequest(
    string Name,
    string? Color,
    string? Icon);

namespace FinanceManager.Api.Contracts.Requests.TransactionCategories;

public sealed record CreateTransactionCategoryRequest(
    string Name,
    string Type,
    string? Color,
    string? Icon);

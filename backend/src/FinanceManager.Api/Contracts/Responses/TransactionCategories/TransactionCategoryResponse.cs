namespace FinanceManager.Api.Contracts.Responses.TransactionCategories;

public sealed record TransactionCategoryResponse(
    Guid Id,
    string Name,
    string Type,
    string? Color,
    string? Icon,
    bool IsSystem,
    bool IsActive,
    DateTime CreatedAtUtc);

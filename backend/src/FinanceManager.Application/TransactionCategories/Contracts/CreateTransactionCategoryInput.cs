using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.TransactionCategories.Contracts;

public sealed record CreateTransactionCategoryInput(
    Guid UserId,
    string Name,
    TransactionCategoryType Type,
    string? Color,
    string? Icon);

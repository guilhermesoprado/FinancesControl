using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.TransactionCategories.Contracts;

public sealed record TransactionCategoryDto(
    Guid Id,
    string Name,
    TransactionCategoryType Type,
    string? Color,
    string? Icon,
    bool IsSystem,
    bool IsActive,
    DateTime CreatedAtUtc);

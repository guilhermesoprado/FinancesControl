using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.FinancialAccounts.Contracts;

public sealed record FinancialAccountDto(
    Guid Id,
    string Name,
    FinancialAccountType Type,
    decimal InitialBalance,
    decimal? CurrentBalanceSnapshot,
    bool IsActive,
    string? InstitutionName,
    string? Description,
    DateTime CreatedAtUtc);

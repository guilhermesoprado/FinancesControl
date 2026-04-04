using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.FinancialAccounts.Contracts;

public sealed record CreateFinancialAccountInput(
    Guid UserId,
    string Name,
    FinancialAccountType Type,
    decimal InitialBalance,
    string? InstitutionName,
    string? Description);

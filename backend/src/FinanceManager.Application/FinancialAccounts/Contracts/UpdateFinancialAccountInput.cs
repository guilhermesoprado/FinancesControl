using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.FinancialAccounts.Contracts;

public sealed record UpdateFinancialAccountInput(
    Guid UserId,
    Guid FinancialAccountId,
    string Name,
    FinancialAccountType Type,
    string? InstitutionName,
    string? Description);

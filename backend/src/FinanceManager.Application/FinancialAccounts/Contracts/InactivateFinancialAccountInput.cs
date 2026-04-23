namespace FinanceManager.Application.FinancialAccounts.Contracts;

public sealed record InactivateFinancialAccountInput(
    Guid UserId,
    Guid FinancialAccountId);

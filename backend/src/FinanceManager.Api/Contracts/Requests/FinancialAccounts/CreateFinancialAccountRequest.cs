namespace FinanceManager.Api.Contracts.Requests.FinancialAccounts;

public sealed record CreateFinancialAccountRequest(
    string Name,
    string Type,
    decimal InitialBalance,
    string? InstitutionName,
    string? Description);

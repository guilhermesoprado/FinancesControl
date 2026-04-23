namespace FinanceManager.Api.Contracts.Requests.FinancialAccounts;

public sealed record UpdateFinancialAccountRequest(
    string Name,
    string Type,
    string? InstitutionName,
    string? Description);

namespace FinanceManager.Api.Contracts.Responses.FinancialAccounts;

public sealed record FinancialAccountResponse(
    Guid Id,
    string Name,
    string Type,
    decimal InitialBalance,
    decimal? CurrentBalanceSnapshot,
    bool IsActive,
    string? InstitutionName,
    string? Description,
    DateTime CreatedAtUtc);

namespace FinanceManager.Api.Contracts.Responses.FinancialOverview;

public sealed record FinancialOverviewResponse(
    string periodFrom,
    string periodTo,
    decimal consolidatedBalance,
    int activeAccountsCount,
    decimal incomeTotal,
    decimal expenseTotal,
    decimal transferTotal,
    IReadOnlyList<FinancialOverviewAccountResponse> accounts,
    IReadOnlyList<FinancialOverviewRecentTransactionResponse> recentTransactions);

public sealed record FinancialOverviewAccountResponse(
    Guid id,
    string name,
    string type,
    decimal visibleBalance,
    string? institutionName,
    bool isActive);

public sealed record FinancialOverviewRecentTransactionResponse(
    Guid id,
    string type,
    string status,
    decimal amount,
    DateOnly occurredOn,
    string? description,
    Guid? financialAccountId,
    Guid? sourceFinancialAccountId,
    Guid? destinationFinancialAccountId);

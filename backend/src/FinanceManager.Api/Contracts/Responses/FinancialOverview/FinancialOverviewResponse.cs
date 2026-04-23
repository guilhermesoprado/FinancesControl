namespace FinanceManager.Api.Contracts.Responses.FinancialOverview;

public sealed record FinancialOverviewResponse(
    string periodFrom,
    string periodTo,
    decimal consolidatedBalance,
    int activeAccountsCount,
    decimal incomeTotal,
    decimal expenseTotal,
    decimal transferTotal,
    FinancialOverviewPeriodComparisonResponse periodComparison,
    IReadOnlyList<FinancialOverviewAccountResponse> accounts,
    IReadOnlyList<FinancialOverviewRecentTransactionResponse> recentTransactions,
    IReadOnlyList<FinancialOverviewAccountPeriodSummaryResponse> accountSummaries,
    IReadOnlyList<FinancialOverviewCategoryPeriodSummaryResponse> categorySummaries);

public sealed record FinancialOverviewPeriodComparisonResponse(
    string previousPeriodFrom,
    string previousPeriodTo,
    decimal previousIncomeTotal,
    decimal previousExpenseTotal,
    decimal previousTransferTotal,
    decimal previousNetResult);

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

public sealed record FinancialOverviewAccountPeriodSummaryResponse(
    Guid accountId,
    string accountName,
    decimal incomeTotal,
    decimal expenseTotal,
    decimal netResult);

public sealed record FinancialOverviewCategoryPeriodSummaryResponse(
    Guid categoryId,
    string categoryName,
    string type,
    decimal totalAmount,
    int transactionsCount);

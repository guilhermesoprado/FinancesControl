using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.FinancialOverview.Contracts;

public sealed record FinancialOverviewDto(
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    decimal ConsolidatedBalance,
    int ActiveAccountsCount,
    decimal IncomeTotal,
    decimal ExpenseTotal,
    decimal TransferTotal,
    FinancialOverviewPeriodComparisonDto PeriodComparison,
    IReadOnlyList<FinancialOverviewAccountDto> Accounts,
    IReadOnlyList<FinancialOverviewRecentTransactionDto> RecentTransactions,
    IReadOnlyList<FinancialOverviewAccountPeriodSummaryDto> AccountSummaries,
    IReadOnlyList<FinancialOverviewCategoryPeriodSummaryDto> CategorySummaries);

public sealed record FinancialOverviewPeriodComparisonDto(
    DateOnly PreviousPeriodFrom,
    DateOnly PreviousPeriodTo,
    decimal PreviousIncomeTotal,
    decimal PreviousExpenseTotal,
    decimal PreviousTransferTotal,
    decimal PreviousNetResult);

public sealed record FinancialOverviewAccountDto(
    Guid Id,
    string Name,
    FinancialAccountType Type,
    decimal VisibleBalance,
    string? InstitutionName,
    bool IsActive);

public sealed record FinancialOverviewRecentTransactionDto(
    Guid Id,
    TransactionType Type,
    TransactionStatus Status,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description,
    Guid? FinancialAccountId,
    Guid? SourceFinancialAccountId,
    Guid? DestinationFinancialAccountId);

public sealed record FinancialOverviewAccountPeriodSummaryDto(
    Guid AccountId,
    string AccountName,
    decimal IncomeTotal,
    decimal ExpenseTotal,
    decimal NetResult);

public sealed record FinancialOverviewCategoryPeriodSummaryDto(
    Guid CategoryId,
    string CategoryName,
    TransactionType Type,
    decimal TotalAmount,
    int TransactionsCount);

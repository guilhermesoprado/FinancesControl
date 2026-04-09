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
    IReadOnlyList<FinancialOverviewAccountDto> Accounts,
    IReadOnlyList<FinancialOverviewRecentTransactionDto> RecentTransactions);

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

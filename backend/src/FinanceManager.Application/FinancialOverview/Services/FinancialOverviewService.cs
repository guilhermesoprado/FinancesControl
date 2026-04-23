using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.FinancialOverview.Contracts;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.FinancialOverview.Services;

public sealed class FinancialOverviewService : IFinancialOverviewService
{
    private readonly IFinancialAccountRepository _financialAccountRepository;
    private readonly ITransactionCategoryRepository _transactionCategoryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public FinancialOverviewService(
        IFinancialAccountRepository financialAccountRepository,
        ITransactionCategoryRepository transactionCategoryRepository,
        ITransactionRepository transactionRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _financialAccountRepository = financialAccountRepository;
        _transactionCategoryRepository = transactionCategoryRepository;
        _transactionRepository = transactionRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<FinancialOverviewDto> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para carregar a visao financeira.");
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var periodFrom = new DateOnly(today.Year, today.Month, 1);
        var currentPeriodLength = today.DayNumber - periodFrom.DayNumber + 1;
        var previousPeriodTo = periodFrom.AddDays(-1);
        var previousPeriodFrom = previousPeriodTo.AddDays(-(currentPeriodLength - 1));

        var accounts = await _financialAccountRepository.GetByUserIdAsync(userId, cancellationToken);
        var categories = await _transactionCategoryRepository.GetByUserIdAsync(userId, cancellationToken);
        var transactions = await _transactionRepository.GetByUserAndPeriodAsync(
            userId,
            periodFrom,
            today,
            null,
            null,
            cancellationToken);
        var previousTransactions = await _transactionRepository.GetByUserAndPeriodAsync(
            userId,
            previousPeriodFrom,
            previousPeriodTo,
            null,
            null,
            cancellationToken);

        var consolidatedBalance = accounts
            .Where(x => x.IsActive)
            .Sum(x => x.CurrentBalanceSnapshot ?? x.InitialBalance);

        var incomeTotal = transactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
        var expenseTotal = transactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);
        var transferTotal = transactions.Where(x => x.Type == TransactionType.Transfer).Sum(x => x.Amount);
        var previousIncomeTotal = previousTransactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
        var previousExpenseTotal = previousTransactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);
        var previousTransferTotal = previousTransactions.Where(x => x.Type == TransactionType.Transfer).Sum(x => x.Amount);

        return new FinancialOverviewDto(
            periodFrom,
            today,
            consolidatedBalance,
            accounts.Count(x => x.IsActive),
            incomeTotal,
            expenseTotal,
            transferTotal,
            new FinancialOverviewPeriodComparisonDto(
                previousPeriodFrom,
                previousPeriodTo,
                previousIncomeTotal,
                previousExpenseTotal,
                previousTransferTotal,
                previousIncomeTotal - previousExpenseTotal),
            accounts.OrderBy(x => x.Name).Select(MapAccount).ToList(),
            transactions.Take(5).Select(MapTransaction).ToList(),
            BuildAccountSummaries(accounts, transactions),
            BuildCategorySummaries(categories, transactions));
    }

    private static FinancialOverviewAccountDto MapAccount(FinancialAccount account)
    {
        return new FinancialOverviewAccountDto(
            account.Id,
            account.Name,
            account.Type,
            account.CurrentBalanceSnapshot ?? account.InitialBalance,
            account.InstitutionName,
            account.IsActive);
    }

    private static FinancialOverviewRecentTransactionDto MapTransaction(Transaction transaction)
    {
        return new FinancialOverviewRecentTransactionDto(
            transaction.Id,
            transaction.Type,
            transaction.Status,
            transaction.Amount,
            transaction.OccurredOn,
            transaction.Description,
            transaction.FinancialAccountId,
            transaction.SourceFinancialAccountId,
            transaction.DestinationFinancialAccountId);
    }

    private static IReadOnlyList<FinancialOverviewAccountPeriodSummaryDto> BuildAccountSummaries(
        IReadOnlyList<FinancialAccount> accounts,
        IReadOnlyList<Transaction> transactions)
    {
        var accountNames = accounts.ToDictionary(x => x.Id, x => x.Name);

        return transactions
            .Where(x =>
                x.FinancialAccountId.HasValue &&
                (x.Type == TransactionType.Income || x.Type == TransactionType.Expense))
            .GroupBy(x => x.FinancialAccountId!.Value)
            .Select(group =>
            {
                var income = group
                    .Where(x => x.Type == TransactionType.Income)
                    .Sum(x => x.Amount);
                var expense = group
                    .Where(x => x.Type == TransactionType.Expense)
                    .Sum(x => x.Amount);

                return new FinancialOverviewAccountPeriodSummaryDto(
                    group.Key,
                    accountNames.TryGetValue(group.Key, out var name) ? name : "Conta nao encontrada",
                    income,
                    expense,
                    income - expense);
            })
            .OrderByDescending(x => Math.Abs(x.NetResult))
            .ThenBy(x => x.AccountName)
            .Take(4)
            .ToList();
    }

    private static IReadOnlyList<FinancialOverviewCategoryPeriodSummaryDto> BuildCategorySummaries(
        IReadOnlyList<TransactionCategory> categories,
        IReadOnlyList<Transaction> transactions)
    {
        var categoryNames = categories.ToDictionary(x => x.Id, x => x.Name);

        return transactions
            .Where(x =>
                x.TransactionCategoryId.HasValue &&
                (x.Type == TransactionType.Income || x.Type == TransactionType.Expense))
            .GroupBy(x => new { CategoryId = x.TransactionCategoryId!.Value, x.Type })
            .Select(group => new FinancialOverviewCategoryPeriodSummaryDto(
                group.Key.CategoryId,
                categoryNames.TryGetValue(group.Key.CategoryId, out var name)
                    ? name
                    : "Categoria nao encontrada",
                group.Key.Type,
                group.Sum(x => x.Amount),
                group.Count()))
            .OrderByDescending(x => x.TotalAmount)
            .ThenBy(x => x.CategoryName)
            .Take(6)
            .ToList();
    }
}

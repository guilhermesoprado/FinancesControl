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
    private readonly ITransactionRepository _transactionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public FinancialOverviewService(
        IFinancialAccountRepository financialAccountRepository,
        ITransactionRepository transactionRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _financialAccountRepository = financialAccountRepository;
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

        var accounts = await _financialAccountRepository.GetByUserIdAsync(userId, cancellationToken);
        var transactions = await _transactionRepository.GetByUserAndPeriodAsync(
            userId,
            periodFrom,
            today,
            null,
            null,
            cancellationToken);

        var consolidatedBalance = accounts
            .Where(x => x.IsActive)
            .Sum(x => x.CurrentBalanceSnapshot ?? x.InitialBalance);

        var incomeTotal = transactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
        var expenseTotal = transactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);
        var transferTotal = transactions.Where(x => x.Type == TransactionType.Transfer).Sum(x => x.Amount);

        return new FinancialOverviewDto(
            periodFrom,
            today,
            consolidatedBalance,
            accounts.Count(x => x.IsActive),
            incomeTotal,
            expenseTotal,
            transferTotal,
            accounts.OrderBy(x => x.Name).Select(MapAccount).ToList(),
            transactions.Take(5).Select(MapTransaction).ToList());
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
}

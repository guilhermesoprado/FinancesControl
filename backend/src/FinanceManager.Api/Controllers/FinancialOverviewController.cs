using System.Security.Claims;
using FinanceManager.Api.Contracts.Responses.FinancialOverview;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.FinancialOverview;
using FinanceManager.Application.FinancialOverview.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/financial-overview")]
public sealed class FinancialOverviewController : ControllerBase
{
    private readonly IFinancialOverviewService _financialOverviewService;

    public FinancialOverviewController(IFinancialOverviewService financialOverviewService)
    {
        _financialOverviewService = financialOverviewService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(FinancialOverviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinancialOverviewResponse>> Get(CancellationToken cancellationToken)
    {
        var overview = await _financialOverviewService.GetAsync(GetAuthenticatedUserId(), cancellationToken);
        return Ok(MapResponse(overview));
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Sid)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado.");
        }

        return userId;
    }

    private static FinancialOverviewResponse MapResponse(FinancialOverviewDto overview)
    {
        return new FinancialOverviewResponse(
            overview.PeriodFrom.ToString("yyyy-MM-dd"),
            overview.PeriodTo.ToString("yyyy-MM-dd"),
            overview.ConsolidatedBalance,
            overview.ActiveAccountsCount,
            overview.IncomeTotal,
            overview.ExpenseTotal,
            overview.TransferTotal,
            MapPeriodComparison(overview.PeriodComparison),
            overview.Accounts.Select(MapAccount).ToList(),
            overview.RecentTransactions.Select(MapTransaction).ToList(),
            overview.AccountSummaries.Select(MapAccountSummary).ToList(),
            overview.CategorySummaries.Select(MapCategorySummary).ToList());
    }

    private static FinancialOverviewPeriodComparisonResponse MapPeriodComparison(FinancialOverviewPeriodComparisonDto comparison)
    {
        return new FinancialOverviewPeriodComparisonResponse(
            comparison.PreviousPeriodFrom.ToString("yyyy-MM-dd"),
            comparison.PreviousPeriodTo.ToString("yyyy-MM-dd"),
            comparison.PreviousIncomeTotal,
            comparison.PreviousExpenseTotal,
            comparison.PreviousTransferTotal,
            comparison.PreviousNetResult);
    }

    private static FinancialOverviewAccountResponse MapAccount(FinancialOverviewAccountDto account)
    {
        return new FinancialOverviewAccountResponse(
            account.Id,
            account.Name,
            MapAccountType(account.Type),
            account.VisibleBalance,
            account.InstitutionName,
            account.IsActive);
    }

    private static FinancialOverviewRecentTransactionResponse MapTransaction(FinancialOverviewRecentTransactionDto transaction)
    {
        return new FinancialOverviewRecentTransactionResponse(
            transaction.Id,
            MapTransactionType(transaction.Type),
            MapTransactionStatus(transaction.Status),
            transaction.Amount,
            transaction.OccurredOn,
            transaction.Description,
            transaction.FinancialAccountId,
            transaction.SourceFinancialAccountId,
            transaction.DestinationFinancialAccountId);
    }

    private static FinancialOverviewAccountPeriodSummaryResponse MapAccountSummary(FinancialOverviewAccountPeriodSummaryDto summary)
    {
        return new FinancialOverviewAccountPeriodSummaryResponse(
            summary.AccountId,
            summary.AccountName,
            summary.IncomeTotal,
            summary.ExpenseTotal,
            summary.NetResult);
    }

    private static FinancialOverviewCategoryPeriodSummaryResponse MapCategorySummary(FinancialOverviewCategoryPeriodSummaryDto summary)
    {
        return new FinancialOverviewCategoryPeriodSummaryResponse(
            summary.CategoryId,
            summary.CategoryName,
            MapTransactionType(summary.Type),
            summary.TotalAmount,
            summary.TransactionsCount);
    }

    private static string MapAccountType(FinancialAccountType type)
    {
        return type switch
        {
            FinancialAccountType.BankAccount => "bank_account",
            FinancialAccountType.Wallet => "wallet",
            FinancialAccountType.InvestmentAccount => "investment_account",
            _ => throw new AppValidationException("O tipo de conta financeira nao e suportado.")
        };
    }

    private static string MapTransactionType(TransactionType type)
    {
        return type switch
        {
            TransactionType.Income => "income",
            TransactionType.Expense => "expense",
            TransactionType.Transfer => "transfer",
            _ => throw new AppValidationException("O tipo de transacao nao e suportado.")
        };
    }

    private static string MapTransactionStatus(TransactionStatus status)
    {
        return status switch
        {
            TransactionStatus.Posted => "posted",
            TransactionStatus.Scheduled => "scheduled",
            _ => throw new AppValidationException("O status da transacao nao e suportado.")
        };
    }
}

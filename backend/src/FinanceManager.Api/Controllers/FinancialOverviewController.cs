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
            overview.Accounts.Select(MapAccount).ToList(),
            overview.RecentTransactions.Select(MapTransaction).ToList());
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

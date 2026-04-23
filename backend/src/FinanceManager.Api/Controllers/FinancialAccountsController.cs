using System.Security.Claims;
using FinanceManager.Api.Contracts.Requests.FinancialAccounts;
using FinanceManager.Api.Contracts.Responses.FinancialAccounts;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.FinancialAccounts;
using FinanceManager.Application.FinancialAccounts.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/financial-accounts")]
public sealed class FinancialAccountsController : ControllerBase
{
    private readonly IFinancialAccountService _financialAccountService;

    public FinancialAccountsController(IFinancialAccountService financialAccountService)
    {
        _financialAccountService = financialAccountService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(FinancialAccountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinancialAccountResponse>> Create(
        [FromBody] CreateFinancialAccountRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        var account = await _financialAccountService.CreateAsync(
            new CreateFinancialAccountInput(
                userId,
                request.Name,
                MapType(request.Type),
                request.InitialBalance,
                request.InstitutionName,
                request.Description),
            cancellationToken);

        return Ok(MapResponse(account));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FinancialAccountResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FinancialAccountResponse>>> Get(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var accounts = await _financialAccountService.GetByUserAsync(userId, cancellationToken);

        return Ok(accounts.Select(MapResponse).ToList());
    }

    [HttpPut("{financialAccountId:guid}")]
    [ProducesResponseType(typeof(FinancialAccountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinancialAccountResponse>> Update(
        Guid financialAccountId,
        [FromBody] UpdateFinancialAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await _financialAccountService.UpdateAsync(
            new UpdateFinancialAccountInput(
                GetAuthenticatedUserId(),
                financialAccountId,
                request.Name,
                MapType(request.Type),
                request.InstitutionName,
                request.Description),
            cancellationToken);

        return Ok(MapResponse(account));
    }

    [HttpPost("{financialAccountId:guid}/inactivate")]
    [ProducesResponseType(typeof(FinancialAccountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinancialAccountResponse>> Inactivate(
        Guid financialAccountId,
        CancellationToken cancellationToken)
    {
        var account = await _financialAccountService.InactivateAsync(
            new InactivateFinancialAccountInput(GetAuthenticatedUserId(), financialAccountId),
            cancellationToken);

        return Ok(MapResponse(account));
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

    private static FinancialAccountType MapType(string type)
    {
        return type.Trim().ToLowerInvariant() switch
        {
            "bank_account" => FinancialAccountType.BankAccount,
            "wallet" => FinancialAccountType.Wallet,
            "investment_account" => FinancialAccountType.InvestmentAccount,
            _ => throw new AppValidationException("O tipo de conta informado e invalido.")
        };
    }

    private static FinancialAccountResponse MapResponse(FinancialAccountDto account)
    {
        return new FinancialAccountResponse(
            account.Id,
            account.Name,
            MapType(account.Type),
            account.InitialBalance,
            account.CurrentBalanceSnapshot,
            account.IsActive,
            account.InstitutionName,
            account.Description,
            account.CreatedAtUtc);
    }

    private static string MapType(FinancialAccountType type)
    {
        return type switch
        {
            FinancialAccountType.BankAccount => "bank_account",
            FinancialAccountType.Wallet => "wallet",
            FinancialAccountType.InvestmentAccount => "investment_account",
            _ => throw new AppValidationException("O tipo de conta financeira nao e suportado.")
        };
    }
}

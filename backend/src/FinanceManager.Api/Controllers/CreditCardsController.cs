using System.Security.Claims;
using FinanceManager.Api.Contracts.Requests.CreditCards;
using FinanceManager.Api.Contracts.Responses.CreditCards;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.CreditCards;
using FinanceManager.Application.CreditCards.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/credit-cards")]
public sealed class CreditCardsController : ControllerBase
{
    private readonly ICreditCardService _creditCardService;

    public CreditCardsController(ICreditCardService creditCardService)
    {
        _creditCardService = creditCardService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreditCardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreditCardResponse>> Create(
        [FromBody] CreateCreditCardRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        var creditCard = await _creditCardService.CreateAsync(
            new CreateCreditCardInput(
                userId,
                request.Name,
                request.Brand,
                request.CreditLimit,
                request.ClosingDay,
                request.DueDay,
                request.Description),
            cancellationToken);

        return Ok(MapResponse(creditCard));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CreditCardResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CreditCardResponse>>> Get(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var creditCards = await _creditCardService.GetByUserAsync(userId, cancellationToken);

        return Ok(creditCards.Select(MapResponse).ToList());
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(IReadOnlyList<CreditCardOverviewResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CreditCardOverviewResponse>>> GetOverview(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var creditCards = await _creditCardService.GetOverviewByUserAsync(userId, cancellationToken);

        return Ok(creditCards.Select(MapOverviewResponse).ToList());
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

    private static CreditCardResponse MapResponse(CreditCardDto creditCard)
    {
        return new CreditCardResponse(
            creditCard.Id,
            creditCard.Name,
            creditCard.Brand,
            creditCard.CreditLimit,
            creditCard.ClosingDay,
            creditCard.DueDay,
            creditCard.IsActive,
            creditCard.Description,
            creditCard.CreatedAtUtc);
    }

    private static CreditCardOverviewResponse MapOverviewResponse(CreditCardOverviewDto creditCard)
    {
        return new CreditCardOverviewResponse(
            creditCard.CreditCardId,
            creditCard.Name,
            creditCard.Brand,
            creditCard.CreditLimit,
            creditCard.ClosingDay,
            creditCard.DueDay,
            creditCard.IsActive,
            creditCard.Description,
            creditCard.OpenInvoiceAmount,
            creditCard.OpenInvoicesCount,
            creditCard.TotalInvoicesCount,
            creditCard.TotalPurchasesAmount,
            creditCard.TotalPurchasesCount,
            creditCard.LatestInvoiceReferenceYear,
            creditCard.LatestInvoiceReferenceMonth,
            creditCard.LastPurchaseOn,
            creditCard.CreatedAtUtc);
    }
}

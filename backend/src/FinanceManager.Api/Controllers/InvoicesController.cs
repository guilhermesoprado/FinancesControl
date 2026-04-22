using System.Security.Claims;
using FinanceManager.Api.Contracts.Requests.Invoices;
using FinanceManager.Api.Contracts.Responses.Invoices;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.Invoices;
using FinanceManager.Application.Invoices.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceResponse>> Create(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.CreateAsync(
            new CreateInvoiceInput(
                GetAuthenticatedUserId(),
                request.CreditCardId,
                request.ReferenceYear,
                request.ReferenceMonth),
            cancellationToken);

        return Ok(MapResponse(invoice));
    }

    [HttpPost("expenses")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceResponse>> RegisterCardExpense(
        [FromBody] RegisterCardExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.RegisterCardExpenseAsync(
            new RegisterCardExpenseInput(
                GetAuthenticatedUserId(),
                request.CreditCardId,
                request.TransactionCategoryId,
                request.Amount,
                request.OccurredOn,
                request.Description,
                request.TargetInvoiceId,
                request.InstallmentCount),
            cancellationToken);

        return Ok(MapResponse(invoice));
    }

    [HttpGet("expenses")]
    [ProducesResponseType(typeof(IReadOnlyList<CreditCardExpenseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CreditCardExpenseResponse>>> GetCardExpenses(
        [FromQuery] Guid? creditCardId,
        [FromQuery] Guid? invoiceId,
        CancellationToken cancellationToken)
    {
        var expenses = await _invoiceService.GetCardExpensesByUserAsync(
            GetAuthenticatedUserId(),
            creditCardId,
            invoiceId,
            cancellationToken);

        return Ok(expenses.Select(MapExpenseResponse).ToList());
    }

    [HttpPost("{invoiceId:guid}/close")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceResponse>> Close(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.CloseAsync(GetAuthenticatedUserId(), invoiceId, cancellationToken);
        return Ok(MapResponse(invoice));
    }

    [HttpPost("{invoiceId:guid}/pay")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceResponse>> Pay(
        Guid invoiceId,
        [FromBody] PayInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.PayAsync(
            new PayInvoiceInput(
                GetAuthenticatedUserId(),
                invoiceId,
                request.FinancialAccountId,
                request.Amount),
            cancellationToken);

        return Ok(MapResponse(invoice));
    }

    [HttpPost("{invoiceId:guid}/adjustments")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceResponse>> Adjust(
        Guid invoiceId,
        [FromBody] AdjustInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.AdjustAsync(
            new AdjustInvoiceInput(
                GetAuthenticatedUserId(),
                invoiceId,
                ParseAdjustmentType(request.AdjustmentType),
                request.Amount),
            cancellationToken);

        return Ok(MapResponse(invoice));
    }
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvoiceResponse>>> Get(
        [FromQuery] Guid? creditCardId,
        CancellationToken cancellationToken)
    {
        var invoices = await _invoiceService.GetByUserAsync(GetAuthenticatedUserId(), creditCardId, cancellationToken);
        return Ok(invoices.Select(MapResponse).ToList());
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

    private static CreditCardExpenseResponse MapExpenseResponse(CreditCardExpenseDto expense)
    {
        return new CreditCardExpenseResponse(
            expense.Id,
            expense.CreditCardId,
            expense.InvoiceId,
            expense.TransactionCategoryId,
            expense.TransactionCategoryName,
            expense.InstallmentGroupId,
            expense.InstallmentNumber,
            expense.InstallmentCount,
            expense.Amount,
            expense.OccurredOn,
            expense.Description,
            expense.CreatedAtUtc);
    }

    private static InvoiceAdjustmentType ParseAdjustmentType(string adjustmentType)
    {
        return adjustmentType.Trim().ToLowerInvariant() switch
        {
            "credit" => InvoiceAdjustmentType.Credit,
            "discount" => InvoiceAdjustmentType.Discount,
            "fee" => InvoiceAdjustmentType.Fee,
            "interest" => InvoiceAdjustmentType.Interest,
            "penalty" => InvoiceAdjustmentType.Penalty,
            "manualincrease" => InvoiceAdjustmentType.ManualIncrease,
            "manualdecrease" => InvoiceAdjustmentType.ManualDecrease,
            _ => throw new AppValidationException("O tipo de ajuste informado nao e suportado.")
        };
    }
    private static InvoiceResponse MapResponse(InvoiceDto invoice)
    {
        return new InvoiceResponse(
            invoice.Id,
            invoice.CreditCardId,
            invoice.CreditCardName,
            invoice.CreditCardBrand,
            invoice.ReferenceYear,
            invoice.ReferenceMonth,
            invoice.PeriodStart,
            invoice.PeriodEnd,
            invoice.ClosingDate,
            invoice.DueDate,
            invoice.TotalAmount,
            invoice.PaidAmount,
            invoice.RemainingAmount,
            invoice.SuggestedMinimumPaymentAmount,
            invoice.LateFeeAppliedAmount,
            invoice.LateInterestAppliedAmount,
            invoice.RevolvingInterestAppliedAmount,
            MapStatus(invoice.Status),
            invoice.PaidFromFinancialAccountId,
            invoice.PaidAtUtc,
            invoice.ClosedAtUtc,
            invoice.CreatedAtUtc);
    }

    private static string MapStatus(InvoiceStatus status)
    {
        return status switch
        {
            InvoiceStatus.Open => "open",
            InvoiceStatus.Closed => "closed",
            InvoiceStatus.Paid => "paid",
            InvoiceStatus.PartiallyPaid => "partiallyPaid",
            _ => throw new AppValidationException("O status da fatura nao e suportado.")
        };
    }
}








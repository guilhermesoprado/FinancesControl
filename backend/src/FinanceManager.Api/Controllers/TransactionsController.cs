using System.Security.Claims;
using FinanceManager.Api.Contracts.Requests.Transactions;
using FinanceManager.Api.Contracts.Responses.Transactions;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.Transactions;
using FinanceManager.Application.Transactions.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/transactions")]
public sealed class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost("income")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionResponse>> CreateIncome(
        [FromBody] CreateIncomeTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionService.RegisterIncomeAsync(
            new CreateIncomeTransactionInput(
                GetAuthenticatedUserId(),
                request.FinancialAccountId,
                request.TransactionCategoryId,
                request.Amount,
                request.OccurredOn,
                request.Description),
            cancellationToken);

        return Ok(MapResponse(transaction));
    }

    [HttpPost("expense")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionResponse>> CreateExpense(
        [FromBody] CreateExpenseTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionService.RegisterExpenseAsync(
            new CreateExpenseTransactionInput(
                GetAuthenticatedUserId(),
                request.FinancialAccountId,
                request.TransactionCategoryId,
                request.Amount,
                request.OccurredOn,
                request.Description),
            cancellationToken);

        return Ok(MapResponse(transaction));
    }

    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionResponse>> CreateTransfer(
        [FromBody] CreateTransferTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionService.RegisterTransferAsync(
            new CreateTransferTransactionInput(
                GetAuthenticatedUserId(),
                request.SourceFinancialAccountId,
                request.DestinationFinancialAccountId,
                request.Amount,
                request.OccurredOn,
                request.Description),
            cancellationToken);

        return Ok(MapResponse(transaction));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> Get(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string? type,
        [FromQuery] Guid? financialAccountId,
        CancellationToken cancellationToken)
    {
        var transactions = await _transactionService.GetByPeriodAsync(
            new GetTransactionsByPeriodInput(
                GetAuthenticatedUserId(),
                from,
                to,
                MapNullableType(type),
                financialAccountId),
            cancellationToken);

        return Ok(transactions.Select(MapResponse).ToList());
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

    private static TransactionType? MapNullableType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        return type.Trim().ToLowerInvariant() switch
        {
            "income" => TransactionType.Income,
            "expense" => TransactionType.Expense,
            "transfer" => TransactionType.Transfer,
            _ => throw new AppValidationException("O tipo de transacao informado e invalido.")
        };
    }

    private static TransactionResponse MapResponse(TransactionDto transaction)
    {
        return new TransactionResponse(
            transaction.Id,
            MapType(transaction.Type),
            MapStatus(transaction.Status),
            transaction.Amount,
            transaction.OccurredOn,
            transaction.Description,
            transaction.FinancialAccountId,
            transaction.TransactionCategoryId,
            transaction.SourceFinancialAccountId,
            transaction.DestinationFinancialAccountId,
            transaction.CreatedAtUtc);
    }

    private static string MapType(TransactionType type)
    {
        return type switch
        {
            TransactionType.Income => "income",
            TransactionType.Expense => "expense",
            TransactionType.Transfer => "transfer",
            _ => throw new AppValidationException("O tipo de transacao nao e suportado.")
        };
    }

    private static string MapStatus(TransactionStatus status)
    {
        return status switch
        {
            TransactionStatus.Posted => "posted",
            TransactionStatus.Scheduled => "scheduled",
            _ => throw new AppValidationException("O status da transacao nao e suportado.")
        };
    }
}

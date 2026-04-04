using System.Security.Claims;
using FinanceManager.Api.Contracts.Requests.TransactionCategories;
using FinanceManager.Api.Contracts.Responses.TransactionCategories;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.TransactionCategories;
using FinanceManager.Application.TransactionCategories.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/transaction-categories")]
public sealed class TransactionCategoriesController : ControllerBase
{
    private readonly ITransactionCategoryService _transactionCategoryService;

    public TransactionCategoriesController(ITransactionCategoryService transactionCategoryService)
    {
        _transactionCategoryService = transactionCategoryService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TransactionCategoryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionCategoryResponse>> Create(
        [FromBody] CreateTransactionCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        var transactionCategory = await _transactionCategoryService.CreateAsync(
            new CreateTransactionCategoryInput(
                userId,
                request.Name,
                MapType(request.Type),
                request.Color,
                request.Icon),
            cancellationToken);

        return Ok(MapResponse(transactionCategory));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionCategoryResponse>>> Get(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var categories = await _transactionCategoryService.GetByUserAsync(userId, cancellationToken);

        return Ok(categories.Select(MapResponse).ToList());
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

    private static TransactionCategoryType MapType(string type)
    {
        return type.Trim().ToLowerInvariant() switch
        {
            "expense" => TransactionCategoryType.Expense,
            "income" => TransactionCategoryType.Income,
            _ => throw new AppValidationException("O tipo de categoria informado e invalido.")
        };
    }

    private static TransactionCategoryResponse MapResponse(TransactionCategoryDto transactionCategory)
    {
        return new TransactionCategoryResponse(
            transactionCategory.Id,
            transactionCategory.Name,
            MapType(transactionCategory.Type),
            transactionCategory.Color,
            transactionCategory.Icon,
            transactionCategory.IsSystem,
            transactionCategory.IsActive,
            transactionCategory.CreatedAtUtc);
    }

    private static string MapType(TransactionCategoryType type)
    {
        return type switch
        {
            TransactionCategoryType.Expense => "expense",
            TransactionCategoryType.Income => "income",
            _ => throw new AppValidationException("O tipo de categoria transacional nao e suportado.")
        };
    }
}

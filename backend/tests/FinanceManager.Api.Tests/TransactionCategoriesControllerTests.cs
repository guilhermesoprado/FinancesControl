using System.Security.Claims;
using FinanceManager.Api.Controllers;
using FinanceManager.Application.TransactionCategories;
using FinanceManager.Application.TransactionCategories.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Tests;

public sealed class TransactionCategoriesControllerTests
{
    [Fact]
    public async Task Update_ShouldForwardPayloadToService()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = new TransactionCategoryDto(
            categoryId,
            "Compras",
            TransactionCategoryType.Expense,
            "#ff9900",
            "bag",
            false,
            true,
            new DateTime(2026, 4, 22, 13, 0, 0, DateTimeKind.Utc));
        var service = new FakeTransactionCategoryService { UpdatedCategory = category };
        var controller = CreateController(service, userId);

        var actionResult = await controller.Update(
            categoryId,
            new FinanceManager.Api.Contracts.Requests.TransactionCategories.UpdateTransactionCategoryRequest(
                "Compras",
                "#ff9900",
                "bag"),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        _ = Assert.IsType<FinanceManager.Api.Contracts.Responses.TransactionCategories.TransactionCategoryResponse>(okResult.Value);
        Assert.NotNull(service.LastUpdateInput);
        Assert.Equal(userId, service.LastUpdateInput!.UserId);
        Assert.Equal(categoryId, service.LastUpdateInput.TransactionCategoryId);
        Assert.Equal("Compras", service.LastUpdateInput.Name);
    }

    private static TransactionCategoriesController CreateController(ITransactionCategoryService service, Guid userId)
    {
        return new TransactionCategoriesController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                            "TestAuth"))
                }
            }
        };
    }

    private sealed class FakeTransactionCategoryService : ITransactionCategoryService
    {
        public UpdateTransactionCategoryInput? LastUpdateInput { get; private set; }
        public TransactionCategoryDto? UpdatedCategory { get; set; }

        public Task<TransactionCategoryDto> CreateAsync(CreateTransactionCategoryInput input, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<TransactionCategoryDto> UpdateAsync(UpdateTransactionCategoryInput input, CancellationToken cancellationToken)
        {
            LastUpdateInput = input;
            return Task.FromResult(UpdatedCategory!);
        }

        public Task<TransactionCategoryDto> InactivateAsync(InactivateTransactionCategoryInput input, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<TransactionCategoryDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}

using System.Security.Claims;
using FinanceManager.Api.Controllers;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.Transactions;
using FinanceManager.Application.Transactions.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Tests;

public sealed class TransactionsControllerTests
{
    [Fact]
    public async Task Get_ShouldReturnMappedTransactionResponses()
    {
        var userId = Guid.NewGuid();
        var expectedTransaction = new TransactionDto(
            Guid.NewGuid(),
            TransactionType.Transfer,
            TransactionStatus.Posted,
            90m,
            new DateOnly(2026, 4, 8),
            "Movimentacao",
            null,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 4, 8, 13, 0, 0, DateTimeKind.Utc));
        var service = new FakeTransactionService
        {
            TransactionsToReturn = [expectedTransaction]
        };
        var controller = CreateController(service, userId);

        var actionResult = await controller.Get(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30),
            "transfer",
            null,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<FinanceManager.Api.Contracts.Responses.Transactions.TransactionResponse>>(okResult.Value);
        var response = Assert.Single(payload);

        Assert.Equal("transfer", response.Type);
        Assert.Equal("posted", response.Status);
        Assert.Equal(expectedTransaction.SourceFinancialAccountId, response.SourceFinancialAccountId);
        Assert.Equal(expectedTransaction.DestinationFinancialAccountId, response.DestinationFinancialAccountId);
        Assert.NotNull(service.LastGetByPeriodInput);
        Assert.Equal(TransactionType.Transfer, service.LastGetByPeriodInput!.Type);
        Assert.Equal(userId, service.LastGetByPeriodInput.UserId);
    }

    [Fact]
    public async Task Get_ShouldRejectUnknownTransactionType()
    {
        var controller = CreateController(new FakeTransactionService(), Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => controller.Get(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30),
            "invalid-type",
            null,
            CancellationToken.None));

        Assert.Equal("O tipo de transacao informado e invalido.", exception.Message);
    }

    private static TransactionsController CreateController(ITransactionService transactionService, Guid userId)
    {
        var controller = new TransactionsController(transactionService)
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

        return controller;
    }

    private sealed class FakeTransactionService : ITransactionService
    {
        public IReadOnlyList<TransactionDto> TransactionsToReturn { get; set; } = [];
        public GetTransactionsByPeriodInput? LastGetByPeriodInput { get; private set; }

        public Task<TransactionDto> RegisterIncomeAsync(CreateIncomeTransactionInput input, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TransactionDto> RegisterExpenseAsync(CreateExpenseTransactionInput input, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TransactionDto> RegisterTransferAsync(CreateTransferTransactionInput input, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<TransactionDto>> GetByPeriodAsync(GetTransactionsByPeriodInput input, CancellationToken cancellationToken)
        {
            LastGetByPeriodInput = input;
            return Task.FromResult(TransactionsToReturn);
        }
    }
}

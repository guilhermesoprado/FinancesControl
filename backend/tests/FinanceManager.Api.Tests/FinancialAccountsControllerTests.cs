using System.Security.Claims;
using FinanceManager.Api.Controllers;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.FinancialAccounts;
using FinanceManager.Application.FinancialAccounts.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Tests;

public sealed class FinancialAccountsControllerTests
{
    [Fact]
    public async Task Update_ShouldForwardParsedPayloadToService()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = new FinancialAccountDto(
            accountId,
            "Conta principal",
            FinancialAccountType.Wallet,
            0m,
            0m,
            true,
            "Banco B",
            "Descricao",
            new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc));
        var service = new FakeFinancialAccountService { UpdatedAccount = account };
        var controller = CreateController(service, userId);

        var actionResult = await controller.Update(
            accountId,
            new FinanceManager.Api.Contracts.Requests.FinancialAccounts.UpdateFinancialAccountRequest(
                "Conta principal",
                "wallet",
                "Banco B",
                "Descricao"),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        _ = Assert.IsType<FinanceManager.Api.Contracts.Responses.FinancialAccounts.FinancialAccountResponse>(okResult.Value);
        Assert.NotNull(service.LastUpdateInput);
        Assert.Equal(userId, service.LastUpdateInput!.UserId);
        Assert.Equal(accountId, service.LastUpdateInput.FinancialAccountId);
        Assert.Equal(FinancialAccountType.Wallet, service.LastUpdateInput.Type);
    }

    [Fact]
    public async Task Update_ShouldRejectUnknownType()
    {
        var controller = CreateController(new FakeFinancialAccountService(), Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => controller.Update(
            Guid.NewGuid(),
            new FinanceManager.Api.Contracts.Requests.FinancialAccounts.UpdateFinancialAccountRequest(
                "Conta",
                "invalid-type",
                null,
                null),
            CancellationToken.None));

        Assert.Equal("O tipo de conta informado e invalido.", exception.Message);
    }

    private static FinancialAccountsController CreateController(IFinancialAccountService service, Guid userId)
    {
        return new FinancialAccountsController(service)
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

    private sealed class FakeFinancialAccountService : IFinancialAccountService
    {
        public UpdateFinancialAccountInput? LastUpdateInput { get; private set; }
        public FinancialAccountDto? UpdatedAccount { get; set; }

        public Task<FinancialAccountDto> CreateAsync(CreateFinancialAccountInput input, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<FinancialAccountDto> UpdateAsync(UpdateFinancialAccountInput input, CancellationToken cancellationToken)
        {
            LastUpdateInput = input;
            return Task.FromResult(UpdatedAccount!);
        }

        public Task<FinancialAccountDto> InactivateAsync(InactivateFinancialAccountInput input, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<FinancialAccountDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}

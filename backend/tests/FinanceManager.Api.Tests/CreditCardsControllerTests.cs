using System.Security.Claims;
using FinanceManager.Api.Controllers;
using FinanceManager.Application.CreditCards;
using FinanceManager.Application.CreditCards.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Tests;

public sealed class CreditCardsControllerTests
{
    [Fact]
    public async Task Get_ShouldReturnMappedCreditCardResponses()
    {
        var userId = Guid.NewGuid();
        var service = new FakeCreditCardService
        {
            CreditCards =
            [
                new CreditCardDto(Guid.NewGuid(), "Cartao principal", "Visa", 2500m, 12, 20, true, "Uso principal", new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc))
            ]
        };
        var controller = CreateController(service, userId);

        var result = await controller.Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<FinanceManager.Api.Contracts.Responses.CreditCards.CreditCardResponse>>(ok.Value);
        var single = Assert.Single(payload);

        Assert.Equal("Cartao principal", single.Name);
        Assert.Equal(2500m, single.CreditLimit);
        Assert.Equal(12, single.ClosingDay);
    }

    [Fact]
    public async Task GetOverview_ShouldReturnMappedCreditCardOverviewResponses()
    {
        var userId = Guid.NewGuid();
        var service = new FakeCreditCardService
        {
            Overview =
            [
                new CreditCardOverviewDto(Guid.NewGuid(), "Cartao principal", "Visa", 2500m, 12, 20, true, "Uso principal", 320m, 1, 2, 320m, 5, 2026, 4, new DateOnly(2026, 4, 9), new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc))
            ]
        };
        var controller = CreateController(service, userId);

        var result = await controller.GetOverview(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<FinanceManager.Api.Contracts.Responses.CreditCards.CreditCardOverviewResponse>>(ok.Value);
        var single = Assert.Single(payload);

        Assert.Equal("Cartao principal", single.Name);
        Assert.Equal(320m, single.OpenInvoiceAmount);
        Assert.Equal(5, single.TotalPurchasesCount);
    }

    private static CreditCardsController CreateController(ICreditCardService service, Guid userId)
    {
        return new CreditCardsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "TestAuth"))
                }
            }
        };
    }

    private sealed class FakeCreditCardService : ICreditCardService
    {
        public IReadOnlyList<CreditCardDto> CreditCards { get; set; } = [];
        public IReadOnlyList<CreditCardOverviewDto> Overview { get; set; } = [];

        public Task<CreditCardDto> CreateAsync(CreateCreditCardInput input, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<CreditCardDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(CreditCards);
        }

        public Task<IReadOnlyList<CreditCardOverviewDto>> GetOverviewByUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Overview);
        }
    }
}


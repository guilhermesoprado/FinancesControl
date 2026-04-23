using System.Security.Claims;
using FinanceManager.Api.Controllers;
using FinanceManager.Application.FinancialOverview;
using FinanceManager.Application.FinancialOverview.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Tests;

public sealed class FinancialOverviewControllerTests
{
    [Fact]
    public async Task Get_ShouldReturnMappedOverviewResponse()
    {
        var userId = Guid.NewGuid();
        var service = new FakeFinancialOverviewService
        {
            Overview = new FinancialOverviewDto(
                new DateOnly(2026, 4, 1),
                new DateOnly(2026, 4, 9),
                150m,
                2,
                100m,
                40m,
                10m,
                new FinancialOverviewPeriodComparisonDto(
                    new DateOnly(2026, 3, 1),
                    new DateOnly(2026, 3, 9),
                    80m,
                    35m,
                    5m,
                    45m),
                [new FinancialOverviewAccountDto(Guid.NewGuid(), "Conta", FinancialAccountType.BankAccount, 150m, "Banco", true)],
                [new FinancialOverviewRecentTransactionDto(Guid.NewGuid(), TransactionType.Income, TransactionStatus.Posted, 100m, new DateOnly(2026, 4, 8), "Salario", Guid.NewGuid(), null, null)],
                [new FinancialOverviewAccountPeriodSummaryDto(Guid.NewGuid(), "Conta", 100m, 40m, 60m)],
                [new FinancialOverviewCategoryPeriodSummaryDto(Guid.NewGuid(), "Salario", TransactionType.Income, 100m, 1)]
            )
        };
        var controller = new FinancialOverviewController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "TestAuth"))
                }
            }
        };

        var result = await controller.Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<FinanceManager.Api.Contracts.Responses.FinancialOverview.FinancialOverviewResponse>(ok.Value);

        Assert.Equal("2026-04-01", payload.periodFrom);
        Assert.Equal(150m, payload.consolidatedBalance);
        Assert.Equal("2026-03-01", payload.periodComparison.previousPeriodFrom);
        Assert.Single(payload.accounts);
        Assert.Single(payload.recentTransactions);
        Assert.Single(payload.accountSummaries);
        Assert.Single(payload.categorySummaries);
    }

    private sealed class FakeFinancialOverviewService : IFinancialOverviewService
    {
        public FinancialOverviewDto Overview { get; set; } = new(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 1),
            0m,
            0,
            0m,
            0m,
            0m,
            new FinancialOverviewPeriodComparisonDto(
                new DateOnly(2026, 3, 1),
                new DateOnly(2026, 3, 1),
                0m,
                0m,
                0m,
                0m),
            [],
            [],
            [],
            []);

        public Task<FinancialOverviewDto> GetAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Overview);
        }
    }
}

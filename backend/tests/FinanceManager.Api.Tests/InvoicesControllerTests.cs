using System.Security.Claims;
using FinanceManager.Api.Controllers;
using FinanceManager.Application.Invoices;
using FinanceManager.Application.Invoices.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Tests;

public sealed class InvoicesControllerTests
{
    [Fact]
    public async Task Get_ShouldReturnMappedInvoices()
    {
        var userId = Guid.NewGuid();
        var service = new FakeInvoiceService
        {
            Invoices =
            [
                new InvoiceDto(Guid.NewGuid(), Guid.NewGuid(), "Cartao", "Visa", 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), 0m, 0m, 0m, 0m, 0m, 0m, 0m, InvoiceStatus.Open, null, null, null, new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc))
            ]
        };
        var controller = CreateController(service, userId);

        var result = await controller.Get(null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<FinanceManager.Api.Contracts.Responses.Invoices.InvoiceResponse>>(ok.Value);
        var single = Assert.Single(payload);

        Assert.Equal("Cartao", single.CreditCardName);
        Assert.Equal("open", single.Status);
    }

    [Fact]
    public async Task GetCardExpenses_ShouldReturnMappedExpenses()
    {
        var userId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var service = new FakeInvoiceService
        {
            Expenses =
            [
                new CreditCardExpenseDto(Guid.NewGuid(), Guid.NewGuid(), invoiceId, Guid.NewGuid(), "Mercado", groupId, 2, 5, 120m, new DateOnly(2026, 4, 9), "Compra semanal", new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc))
            ]
        };
        var controller = CreateController(service, userId);

        var result = await controller.GetCardExpenses(null, invoiceId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<FinanceManager.Api.Contracts.Responses.Invoices.CreditCardExpenseResponse>>(ok.Value);
        var single = Assert.Single(payload);

        Assert.Equal("Mercado", single.TransactionCategoryName);
        Assert.Equal(120m, single.Amount);
        Assert.Equal(invoiceId, single.InvoiceId);
        Assert.Equal(2, single.InstallmentNumber);
        Assert.Equal(5, single.InstallmentCount);
        Assert.Equal(groupId, single.InstallmentGroupId);
    }

    private static InvoicesController CreateController(IInvoiceService service, Guid userId)
    {
        return new InvoicesController(service)
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

    private sealed class FakeInvoiceService : IInvoiceService
    {
        public IReadOnlyList<InvoiceDto> Invoices { get; set; } = [];
        public IReadOnlyList<CreditCardExpenseDto> Expenses { get; set; } = [];

        public Task<InvoiceDto> CreateAsync(CreateInvoiceInput input, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InvoiceDto> CloseAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<InvoiceDto>> GetByUserAsync(Guid userId, Guid? creditCardId, CancellationToken cancellationToken) => Task.FromResult(Invoices);
        public Task<IReadOnlyList<CreditCardExpenseDto>> GetCardExpensesByUserAsync(Guid userId, Guid? creditCardId, Guid? invoiceId, CancellationToken cancellationToken) => Task.FromResult(Expenses);
        public Task<InvoiceDto> PayAsync(PayInvoiceInput input, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InvoiceDto> AdjustAsync(AdjustInvoiceInput input, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InvoiceDto> RegisterCardExpenseAsync(RegisterCardExpenseInput input, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}





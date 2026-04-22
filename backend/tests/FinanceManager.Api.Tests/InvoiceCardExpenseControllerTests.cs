using System.Security.Claims;
using FinanceManager.Api.Controllers;
using FinanceManager.Api.Contracts.Requests.Invoices;
using FinanceManager.Application.Invoices;
using FinanceManager.Application.Invoices.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Tests;

public sealed class InvoiceCardExpenseControllerTests
{
    [Fact]
    public async Task RegisterCardExpense_ShouldReturnMappedInvoice()
    {
        var userId = Guid.NewGuid();
        var creditCardId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var service = new FakeInvoiceService
        {
            ExpenseInvoice = new InvoiceDto(Guid.NewGuid(), creditCardId, "Cartao", "Visa", 2026, 5, new DateOnly(2026, 4, 11), new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 18), 240m, 0m, 240m, 36m, 0m, 0m, 0m, InvoiceStatus.Open, null, null, null, new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc))
        };
        var controller = CreateController(service, userId);

        var result = await controller.RegisterCardExpense(
            new RegisterCardExpenseRequest(creditCardId, categoryId, 240m, new DateOnly(2026, 4, 11), "Supermercado", null, 1),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<FinanceManager.Api.Contracts.Responses.Invoices.InvoiceResponse>(ok.Value);
        Assert.Equal(240m, payload.TotalAmount);
        Assert.Equal(5, payload.ReferenceMonth);
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
        public InvoiceDto ExpenseInvoice { get; set; } = default!;

        public Task<InvoiceDto> CreateAsync(CreateInvoiceInput input, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InvoiceDto> CloseAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<InvoiceDto>> GetByUserAsync(Guid userId, Guid? creditCardId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<CreditCardExpenseDto>> GetCardExpensesByUserAsync(Guid userId, Guid? creditCardId, Guid? invoiceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InvoiceDto> PayAsync(PayInvoiceInput input, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InvoiceDto> AdjustAsync(AdjustInvoiceInput input, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InvoiceDto> RegisterCardExpenseAsync(RegisterCardExpenseInput input, CancellationToken cancellationToken) => Task.FromResult(ExpenseInvoice);
    }
}







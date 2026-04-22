using System.Security.Claims;
using FinanceManager.Api.Controllers;
using FinanceManager.Application.Invoices;
using FinanceManager.Application.Invoices.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Tests;

public sealed class InvoicePaymentControllerTests
{
    [Fact]
    public async Task Pay_ShouldReturnMappedPaidInvoice()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var service = new FakeInvoiceService
        {
            PaidInvoice = new InvoiceDto(invoiceId, Guid.NewGuid(), "Cartao", "Visa", 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), 120m, 120m, 0m, 0m, 0m, 0m, 0m, InvoiceStatus.Paid, accountId, new DateTime(2026, 4, 9, 20, 0, 0, DateTimeKind.Utc), null, new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc))
        };
        var controller = CreateController(service, userId);

        var result = await controller.Pay(invoiceId, new FinanceManager.Api.Contracts.Requests.Invoices.PayInvoiceRequest(accountId, 120m), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<FinanceManager.Api.Contracts.Responses.Invoices.InvoiceResponse>(ok.Value);
        Assert.Equal("paid", payload.Status);
        Assert.Equal(accountId, payload.PaidFromFinancialAccountId);
        Assert.Equal(120m, payload.PaidAmount);
        Assert.Equal(0m, payload.RemainingAmount);
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
        public InvoiceDto PaidInvoice { get; set; } = default!;

        public Task<InvoiceDto> CreateAsync(CreateInvoiceInput input, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InvoiceDto> CloseAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<InvoiceDto>> GetByUserAsync(Guid userId, Guid? creditCardId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<CreditCardExpenseDto>> GetCardExpensesByUserAsync(Guid userId, Guid? creditCardId, Guid? invoiceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InvoiceDto> PayAsync(PayInvoiceInput input, CancellationToken cancellationToken) => Task.FromResult(PaidInvoice);
        public Task<InvoiceDto> AdjustAsync(AdjustInvoiceInput input, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InvoiceDto> RegisterCardExpenseAsync(RegisterCardExpenseInput input, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}




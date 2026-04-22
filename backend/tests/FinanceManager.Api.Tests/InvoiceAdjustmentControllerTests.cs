using System.Security.Claims;
using FinanceManager.Api.Controllers;
using FinanceManager.Application.Invoices;
using FinanceManager.Application.Invoices.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Tests;

public sealed class InvoiceAdjustmentControllerTests
{
    [Fact]
    public async Task Adjust_ShouldMapAdjustmentRequestAndReturnAdjustedInvoice()
    {
        var userId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var service = new FakeInvoiceService
        {
            AdjustedInvoice = new InvoiceDto(invoiceId, Guid.NewGuid(), "Cartao", "Visa", 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), 90m, 0m, 90m, 13.5m, 0m, 0m, 0m, InvoiceStatus.Open, null, null, null, new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc))
        };
        var controller = CreateController(service, userId);

        var result = await controller.Adjust(invoiceId, new FinanceManager.Api.Contracts.Requests.Invoices.AdjustInvoiceRequest("discount", 10m), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<FinanceManager.Api.Contracts.Responses.Invoices.InvoiceResponse>(ok.Value);
        Assert.Equal(invoiceId, payload.Id);
        Assert.Equal(90m, payload.TotalAmount);
        Assert.NotNull(service.AdjustInput);
        Assert.Equal(InvoiceAdjustmentType.Discount, service.AdjustInput!.AdjustmentType);
        Assert.Equal(10m, service.AdjustInput.Amount);
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
        public InvoiceDto AdjustedInvoice { get; set; } = default!;
        public AdjustInvoiceInput? AdjustInput { get; private set; }

        public Task<InvoiceDto> CreateAsync(CreateInvoiceInput input, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InvoiceDto> CloseAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<InvoiceDto>> GetByUserAsync(Guid userId, Guid? creditCardId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<CreditCardExpenseDto>> GetCardExpensesByUserAsync(Guid userId, Guid? creditCardId, Guid? invoiceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InvoiceDto> PayAsync(PayInvoiceInput input, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InvoiceDto> AdjustAsync(AdjustInvoiceInput input, CancellationToken cancellationToken)
        {
            AdjustInput = input;
            return Task.FromResult(AdjustedInvoice);
        }
        public Task<InvoiceDto> RegisterCardExpenseAsync(RegisterCardExpenseInput input, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

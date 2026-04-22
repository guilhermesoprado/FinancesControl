using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Tests;

public sealed class InvoiceDomainTests
{
    [Fact]
    public void Open_ShouldCreateOpenInvoiceWithZeroTotal()
    {
        var nowUtc = new DateTime(2026, 4, 9, 14, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var creditCardId = Guid.NewGuid();

        var invoice = Invoice.Open(
            userId,
            creditCardId,
            2026,
            4,
            new DateOnly(2026, 3, 11),
            new DateOnly(2026, 4, 10),
            new DateOnly(2026, 4, 10),
            new DateOnly(2026, 4, 18),
            nowUtc);

        Assert.Equal(userId, invoice.UserId);
        Assert.Equal(creditCardId, invoice.CreditCardId);
        Assert.Equal(InvoiceStatus.Open, invoice.Status);
        Assert.Equal(0m, invoice.TotalAmount);
        Assert.Equal(nowUtc, invoice.CreatedAtUtc);
    }

    [Fact]
    public void Open_ShouldRejectInvalidPeriod()
    {
        var action = () => Invoice.Open(
            Guid.NewGuid(),
            Guid.NewGuid(),
            2026,
            4,
            new DateOnly(2026, 4, 10),
            new DateOnly(2026, 4, 9),
            new DateOnly(2026, 4, 10),
            new DateOnly(2026, 4, 18),
            DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("O periodo da fatura e invalido.", exception.Message);
    }

    [Fact]
    public void AddCharge_ShouldIncreaseTotalAmount()
    {
        var nowUtc = new DateTime(2026, 4, 9, 14, 0, 0, DateTimeKind.Utc);
        var invoice = Invoice.Open(
            Guid.NewGuid(),
            Guid.NewGuid(),
            2026,
            4,
            new DateOnly(2026, 3, 11),
            new DateOnly(2026, 4, 10),
            new DateOnly(2026, 4, 10),
            new DateOnly(2026, 4, 18),
            nowUtc.AddDays(-1));

        invoice.AddCharge(125.75m, nowUtc);

        Assert.Equal(125.75m, invoice.TotalAmount);
        Assert.Equal(nowUtc, invoice.UpdatedAtUtc);
    }
}

using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Tests;

public sealed class InvoicePaymentDomainTests
{
    [Fact]
    public void MarkAsPaid_ShouldSetStatusAndPaymentMetadata()
    {
        var nowUtc = new DateTime(2026, 4, 9, 18, 0, 0, DateTimeKind.Utc);
        var invoice = Invoice.Open(Guid.NewGuid(), Guid.NewGuid(), 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), nowUtc.AddHours(-1));
        var accountId = Guid.NewGuid();

        invoice.MarkAsPaid(accountId, nowUtc);

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(accountId, invoice.PaidFromFinancialAccountId);
        Assert.Equal(nowUtc, invoice.PaidAtUtc);
        Assert.Equal(nowUtc, invoice.UpdatedAtUtc);
    }

    [Fact]
    public void MarkAsPaid_ShouldRejectAlreadyPaidInvoice()
    {
        var invoice = Invoice.Open(Guid.NewGuid(), Guid.NewGuid(), 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), DateTime.UtcNow.AddHours(-1));
        invoice.MarkAsPaid(Guid.NewGuid(), DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(() => invoice.MarkAsPaid(Guid.NewGuid(), DateTime.UtcNow));
        Assert.Equal("A fatura informada ja foi paga.", exception.Message);
    }
}
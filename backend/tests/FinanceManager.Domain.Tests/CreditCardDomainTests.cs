using FinanceManager.Domain.Entities;

namespace FinanceManager.Domain.Tests;

public sealed class CreditCardDomainTests
{
    [Fact]
    public void Create_ShouldTrimOptionalFieldsAndActivateCard()
    {
        var nowUtc = new DateTime(2026, 4, 9, 10, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();

        var creditCard = CreditCard.Create(
            userId,
            "  Cartao principal  ",
            "  Visa  ",
            2500m,
            12,
            20,
            "  Uso recorrente  ",
            nowUtc);

        Assert.Equal(userId, creditCard.UserId);
        Assert.Equal("Cartao principal", creditCard.Name);
        Assert.Equal("Visa", creditCard.Brand);
        Assert.Equal(2500m, creditCard.CreditLimit);
        Assert.Equal(12, creditCard.ClosingDay);
        Assert.Equal(20, creditCard.DueDay);
        Assert.True(creditCard.IsActive);
        Assert.Equal("Uso recorrente", creditCard.Description);
        Assert.Equal(nowUtc, creditCard.CreatedAtUtc);
        Assert.Equal(nowUtc, creditCard.UpdatedAtUtc);
    }

    [Fact]
    public void Create_ShouldRejectInvalidClosingDay()
    {
        var action = () => CreditCard.Create(
            Guid.NewGuid(),
            "Cartao",
            null,
            1000m,
            0,
            10,
            null,
            DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("O dia de fechamento deve estar entre 1 e 31.", exception.Message);
    }
}
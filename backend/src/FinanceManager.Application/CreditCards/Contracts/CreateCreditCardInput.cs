namespace FinanceManager.Application.CreditCards.Contracts;

public sealed record CreateCreditCardInput(
    Guid UserId,
    string Name,
    string? Brand,
    decimal CreditLimit,
    int ClosingDay,
    int DueDay,
    string? Description);
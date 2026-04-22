namespace FinanceManager.Api.Contracts.Requests.CreditCards;

public sealed record CreateCreditCardRequest(
    string Name,
    string? Brand,
    decimal CreditLimit,
    int ClosingDay,
    int DueDay,
    string? Description);
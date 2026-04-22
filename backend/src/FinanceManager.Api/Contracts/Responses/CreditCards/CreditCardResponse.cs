namespace FinanceManager.Api.Contracts.Responses.CreditCards;

public sealed record CreditCardResponse(
    Guid Id,
    string Name,
    string? Brand,
    decimal CreditLimit,
    int ClosingDay,
    int DueDay,
    bool IsActive,
    string? Description,
    DateTime CreatedAtUtc);

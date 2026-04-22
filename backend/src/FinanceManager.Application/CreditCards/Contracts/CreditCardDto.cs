namespace FinanceManager.Application.CreditCards.Contracts;

public sealed record CreditCardDto(
    Guid Id,
    string Name,
    string? Brand,
    decimal CreditLimit,
    int ClosingDay,
    int DueDay,
    bool IsActive,
    string? Description,
    DateTime CreatedAtUtc);

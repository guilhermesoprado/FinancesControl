namespace FinanceManager.Api.Contracts.Responses.CreditCards;

public sealed record CreditCardOverviewResponse(
    Guid CreditCardId,
    string Name,
    string? Brand,
    decimal CreditLimit,
    int ClosingDay,
    int DueDay,
    bool IsActive,
    string? Description,
    decimal OpenInvoiceAmount,
    int OpenInvoicesCount,
    int TotalInvoicesCount,
    decimal TotalPurchasesAmount,
    int TotalPurchasesCount,
    int? LatestInvoiceReferenceYear,
    int? LatestInvoiceReferenceMonth,
    DateOnly? LastPurchaseOn,
    DateTime CreatedAtUtc);

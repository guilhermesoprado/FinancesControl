namespace FinanceManager.Api.Contracts.Responses.Transactions;

public sealed record TransactionResponse(
    Guid Id,
    string Type,
    string Status,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description,
    Guid? FinancialAccountId,
    Guid? TransactionCategoryId,
    Guid? SourceFinancialAccountId,
    Guid? DestinationFinancialAccountId,
    DateTime CreatedAtUtc);

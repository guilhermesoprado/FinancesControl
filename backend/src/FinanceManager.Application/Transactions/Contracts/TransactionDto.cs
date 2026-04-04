using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Transactions.Contracts;

public sealed record TransactionDto(
    Guid Id,
    TransactionType Type,
    TransactionStatus Status,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description,
    Guid? FinancialAccountId,
    Guid? TransactionCategoryId,
    Guid? SourceFinancialAccountId,
    Guid? DestinationFinancialAccountId,
    DateTime CreatedAtUtc);

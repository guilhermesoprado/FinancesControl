namespace FinanceManager.Application.Transactions.Contracts;

public sealed record CreateTransferTransactionInput(
    Guid UserId,
    Guid SourceFinancialAccountId,
    Guid DestinationFinancialAccountId,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description);

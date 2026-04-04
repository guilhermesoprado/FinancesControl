namespace FinanceManager.Api.Contracts.Requests.Transactions;

public sealed record CreateTransferTransactionRequest(
    Guid SourceFinancialAccountId,
    Guid DestinationFinancialAccountId,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description);

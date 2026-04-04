namespace FinanceManager.Api.Contracts.Requests.Transactions;

public sealed record CreateExpenseTransactionRequest(
    Guid FinancialAccountId,
    Guid TransactionCategoryId,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description);

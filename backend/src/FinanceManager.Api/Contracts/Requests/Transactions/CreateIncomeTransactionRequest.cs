namespace FinanceManager.Api.Contracts.Requests.Transactions;

public sealed record CreateIncomeTransactionRequest(
    Guid FinancialAccountId,
    Guid TransactionCategoryId,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description);

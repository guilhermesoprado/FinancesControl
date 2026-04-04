namespace FinanceManager.Application.Transactions.Contracts;

public sealed record CreateExpenseTransactionInput(
    Guid UserId,
    Guid FinancialAccountId,
    Guid TransactionCategoryId,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description);

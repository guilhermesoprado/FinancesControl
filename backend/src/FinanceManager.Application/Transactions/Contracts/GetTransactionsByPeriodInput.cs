using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Transactions.Contracts;

public sealed record GetTransactionsByPeriodInput(
    Guid UserId,
    DateOnly From,
    DateOnly To,
    TransactionType? Type,
    Guid? FinancialAccountId);

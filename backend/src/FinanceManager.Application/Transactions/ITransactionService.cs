using FinanceManager.Application.Transactions.Contracts;

namespace FinanceManager.Application.Transactions;

public interface ITransactionService
{
    Task<TransactionDto> RegisterIncomeAsync(CreateIncomeTransactionInput input, CancellationToken cancellationToken);
    Task<TransactionDto> RegisterExpenseAsync(CreateExpenseTransactionInput input, CancellationToken cancellationToken);
    Task<TransactionDto> RegisterTransferAsync(CreateTransferTransactionInput input, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionDto>> GetByPeriodAsync(GetTransactionsByPeriodInput input, CancellationToken cancellationToken);
}

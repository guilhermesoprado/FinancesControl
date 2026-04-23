using FinanceManager.Application.TransactionCategories.Contracts;

namespace FinanceManager.Application.TransactionCategories;

public interface ITransactionCategoryService
{
    Task<TransactionCategoryDto> CreateAsync(CreateTransactionCategoryInput input, CancellationToken cancellationToken);
    Task<TransactionCategoryDto> UpdateAsync(UpdateTransactionCategoryInput input, CancellationToken cancellationToken);
    Task<TransactionCategoryDto> InactivateAsync(InactivateTransactionCategoryInput input, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionCategoryDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken);
}

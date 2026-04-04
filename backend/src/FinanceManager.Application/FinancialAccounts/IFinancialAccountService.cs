using FinanceManager.Application.FinancialAccounts.Contracts;

namespace FinanceManager.Application.FinancialAccounts;

public interface IFinancialAccountService
{
    Task<FinancialAccountDto> CreateAsync(CreateFinancialAccountInput input, CancellationToken cancellationToken);
    Task<IReadOnlyList<FinancialAccountDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken);
}

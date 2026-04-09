using FinanceManager.Application.FinancialOverview.Contracts;

namespace FinanceManager.Application.FinancialOverview;

public interface IFinancialOverviewService
{
    Task<FinancialOverviewDto> GetAsync(Guid userId, CancellationToken cancellationToken);
}

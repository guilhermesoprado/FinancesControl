using FinanceManager.Application.CreditCards.Contracts;

namespace FinanceManager.Application.CreditCards;

public interface ICreditCardService
{
    Task<CreditCardDto> CreateAsync(CreateCreditCardInput input, CancellationToken cancellationToken);
    Task<IReadOnlyList<CreditCardDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CreditCardOverviewDto>> GetOverviewByUserAsync(Guid userId, CancellationToken cancellationToken);
}

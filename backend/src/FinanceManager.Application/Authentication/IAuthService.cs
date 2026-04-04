using FinanceManager.Application.Authentication.Contracts;

namespace FinanceManager.Application.Authentication;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterUserInput input, CancellationToken cancellationToken);
    Task<AuthResultDto> LoginAsync(LoginInput input, CancellationToken cancellationToken);
    Task<AuthenticatedUserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}

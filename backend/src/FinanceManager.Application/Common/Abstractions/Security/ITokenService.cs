using FinanceManager.Application.Authentication.Contracts;
using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.Common.Abstractions.Security;

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(User user);
}

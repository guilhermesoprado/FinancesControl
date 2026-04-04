namespace FinanceManager.Application.Authentication.Contracts;

public sealed record AuthResultDto(string AccessToken, DateTime ExpiresAtUtc, AuthenticatedUserDto User);

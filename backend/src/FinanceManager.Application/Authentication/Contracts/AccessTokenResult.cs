namespace FinanceManager.Application.Authentication.Contracts;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

namespace FinanceManager.Api.Contracts.Requests.Auth;

public sealed record LoginRequest(string Email, string Password);

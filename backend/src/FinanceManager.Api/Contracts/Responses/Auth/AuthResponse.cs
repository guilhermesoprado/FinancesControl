namespace FinanceManager.Api.Contracts.Responses.Auth;

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, AuthenticatedUserResponse User);

namespace FinanceManager.Api.Contracts.Responses.Auth;

public sealed record AuthenticatedUserResponse(Guid Id, string FullName, string Email);

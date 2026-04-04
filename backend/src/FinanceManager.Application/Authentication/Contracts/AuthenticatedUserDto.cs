namespace FinanceManager.Application.Authentication.Contracts;

public sealed record AuthenticatedUserDto(Guid Id, string FullName, string Email);

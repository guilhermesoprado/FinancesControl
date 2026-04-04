namespace FinanceManager.Application.Authentication.Contracts;

public sealed record RegisterUserInput(string FullName, string Email, string Password);

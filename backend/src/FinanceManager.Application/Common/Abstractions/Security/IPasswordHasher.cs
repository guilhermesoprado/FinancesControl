namespace FinanceManager.Application.Common.Abstractions.Security;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string passwordHash, string providedPassword);
}

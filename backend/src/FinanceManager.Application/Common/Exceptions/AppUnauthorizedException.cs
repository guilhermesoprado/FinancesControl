namespace FinanceManager.Application.Common.Exceptions;

public sealed class AppUnauthorizedException : Exception
{
    public AppUnauthorizedException(string message) : base(message)
    {
    }
}

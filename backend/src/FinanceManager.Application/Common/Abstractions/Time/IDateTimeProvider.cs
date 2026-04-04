namespace FinanceManager.Application.Common.Abstractions.Time;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

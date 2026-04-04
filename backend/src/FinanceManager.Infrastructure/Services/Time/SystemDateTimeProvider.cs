using FinanceManager.Application.Common.Abstractions.Time;

namespace FinanceManager.Infrastructure.Services.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

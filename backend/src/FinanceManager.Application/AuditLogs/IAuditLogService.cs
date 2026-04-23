using FinanceManager.Application.AuditLogs.Contracts;

namespace FinanceManager.Application.AuditLogs;

public interface IAuditLogService
{
    Task<IReadOnlyList<AuditLogDto>> GetByUserAsync(GetAuditLogsInput input, CancellationToken cancellationToken);
}

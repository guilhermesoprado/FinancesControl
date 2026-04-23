using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Common.Abstractions.Persistence;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditLog>> GetByUserAsync(
        Guid userId,
        AuditLogEntityType? entityType,
        AuditLogAction? action,
        Guid? entityId,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        int limit,
        CancellationToken cancellationToken);
}

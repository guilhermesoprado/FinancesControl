using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly FinanceManagerDbContext _dbContext;

    public AuditLogRepository(FinanceManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        return _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(
        Guid userId,
        AuditLogEntityType? entityType,
        AuditLogAction? action,
        Guid? entityId,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditLogs
            .Where(x => x.UserId == userId);

        if (entityType.HasValue)
        {
            query = query.Where(x => x.EntityType == entityType.Value);
        }

        if (action.HasValue)
        {
            query = query.Where(x => x.Action == action.Value);
        }

        if (entityId.HasValue)
        {
            query = query.Where(x => x.EntityId == entityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToUpperInvariant();
            query = query.Where(x => x.Summary.ToUpper().Contains(normalizedSearch));
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc < toUtc.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}

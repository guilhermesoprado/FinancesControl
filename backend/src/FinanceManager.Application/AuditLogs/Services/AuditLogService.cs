using FinanceManager.Application.AuditLogs.Contracts;
using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.AuditLogs.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetByUserAsync(GetAuditLogsInput input, CancellationToken cancellationToken)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para consultar auditoria.");
        }

        if (input.From.HasValue && input.To.HasValue && input.From.Value > input.To.Value)
        {
            throw new AppValidationException("A data inicial da auditoria nao pode ser maior que a data final.");
        }

        if (input.Limit <= 0 || input.Limit > 200)
        {
            throw new AppValidationException("O limite da consulta de auditoria deve estar entre 1 e 200.");
        }

        DateTime? fromUtc = input.From.HasValue
            ? new DateTime(input.From.Value.Year, input.From.Value.Month, input.From.Value.Day, 0, 0, 0, DateTimeKind.Utc)
            : null;
        DateTime? toUtc = input.To.HasValue
            ? new DateTime(input.To.Value.Year, input.To.Value.Month, input.To.Value.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1)
            : null;

        var auditLogs = await _auditLogRepository.GetByUserAsync(
            input.UserId,
            input.EntityType,
            input.Action,
            input.EntityId,
            string.IsNullOrWhiteSpace(input.Search) ? null : input.Search.Trim(),
            fromUtc,
            toUtc,
            input.Limit,
            cancellationToken);

        return auditLogs.Select(Map).ToList();
    }

    private static AuditLogDto Map(AuditLog auditLog)
    {
        return new AuditLogDto(
            auditLog.Id,
            auditLog.UserId,
            auditLog.EntityType,
            auditLog.EntityId,
            auditLog.Action,
            auditLog.Summary,
            auditLog.CreatedAtUtc);
    }
}

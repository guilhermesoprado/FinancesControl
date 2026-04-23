using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.AuditLogs.Contracts;

public sealed record AuditLogDto(
    Guid Id,
    Guid UserId,
    AuditLogEntityType EntityType,
    Guid EntityId,
    AuditLogAction Action,
    string Summary,
    DateTime CreatedAtUtc);

using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.AuditLogs.Contracts;

public sealed record GetAuditLogsInput(
    Guid UserId,
    AuditLogEntityType? EntityType,
    AuditLogAction? Action,
    Guid? EntityId,
    string? Search,
    DateOnly? From,
    DateOnly? To,
    int Limit = 100);

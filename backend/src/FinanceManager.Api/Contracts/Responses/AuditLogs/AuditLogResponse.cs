namespace FinanceManager.Api.Contracts.Responses.AuditLogs;

public sealed record AuditLogResponse(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string Summary,
    DateTime CreatedAtUtc);

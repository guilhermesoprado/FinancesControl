using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public AuditLogEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public AuditLogAction Action { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static AuditLog Create(
        Guid userId,
        AuditLogEntityType entityType,
        Guid entityId,
        AuditLogAction action,
        string summary,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("O usuario do log de auditoria e obrigatorio.");
        }

        if (entityId == Guid.Empty)
        {
            throw new InvalidOperationException("A entidade do log de auditoria e obrigatoria.");
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new InvalidOperationException("O resumo do log de auditoria e obrigatorio.");
        }

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Summary = summary.Trim(),
            CreatedAtUtc = nowUtc
        };
    }
}

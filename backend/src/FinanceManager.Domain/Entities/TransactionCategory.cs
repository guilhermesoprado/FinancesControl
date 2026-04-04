using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Entities;

public sealed class TransactionCategory
{
    private TransactionCategory()
    {
    }

    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TransactionCategoryType Type { get; private set; }
    public string? Color { get; private set; }
    public string? Icon { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static TransactionCategory CreateUserCategory(
        Guid userId,
        string name,
        TransactionCategoryType type,
        string? color,
        string? icon,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("O usuario da categoria transacional e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("O nome da categoria transacional e obrigatorio.");
        }

        return new TransactionCategory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            Type = type,
            Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim(),
            Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim(),
            IsSystem = false,
            IsActive = true,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }
}

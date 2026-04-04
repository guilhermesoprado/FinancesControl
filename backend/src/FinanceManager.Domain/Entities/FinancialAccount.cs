using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Entities;

public sealed class FinancialAccount
{
    private FinancialAccount()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public FinancialAccountType Type { get; private set; }
    public decimal InitialBalance { get; private set; }
    public decimal? CurrentBalanceSnapshot { get; private set; }
    public bool IsActive { get; private set; }
    public string? InstitutionName { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static FinancialAccount Create(
        Guid userId,
        string name,
        FinancialAccountType type,
        decimal initialBalance,
        string? institutionName,
        string? description,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("O usuario da conta financeira e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("O nome da conta financeira e obrigatorio.");
        }

        return new FinancialAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            Type = type,
            InitialBalance = initialBalance,
            CurrentBalanceSnapshot = initialBalance,
            IsActive = true,
            InstitutionName = string.IsNullOrWhiteSpace(institutionName) ? null : institutionName.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }
}

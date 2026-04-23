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

    public void ApplyDelta(decimal delta, DateTime nowUtc)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Nao e possivel movimentar uma conta financeira inativa.");
        }

        if (delta == 0)
        {
            throw new InvalidOperationException("A movimentacao da conta precisa alterar o saldo.");
        }

        CurrentBalanceSnapshot = (CurrentBalanceSnapshot ?? InitialBalance) + delta;
        UpdatedAtUtc = nowUtc;
    }

    public void Update(
        string name,
        FinancialAccountType type,
        string? institutionName,
        string? description,
        DateTime nowUtc)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Nao e possivel editar uma conta financeira inativa.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("O nome da conta financeira e obrigatorio.");
        }

        Name = name.Trim();
        Type = type;
        InstitutionName = string.IsNullOrWhiteSpace(institutionName) ? null : institutionName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void Inactivate(DateTime nowUtc)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("A conta financeira informada ja esta inativa.");
        }

        var currentBalance = CurrentBalanceSnapshot ?? InitialBalance;
        if (currentBalance != 0m)
        {
            throw new InvalidOperationException("Nao e possivel inativar uma conta financeira com saldo visivel diferente de zero.");
        }

        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }
}

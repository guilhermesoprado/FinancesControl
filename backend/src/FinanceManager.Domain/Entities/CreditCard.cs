namespace FinanceManager.Domain.Entities;

public sealed class CreditCard
{
    private CreditCard()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Brand { get; private set; }
    public decimal CreditLimit { get; private set; }
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }
    public bool IsActive { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static CreditCard Create(
        Guid userId,
        string name,
        string? brand,
        decimal creditLimit,
        int closingDay,
        int dueDay,
        string? description,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("O usuario do cartao e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("O nome do cartao e obrigatorio.");
        }

        if (creditLimit < 0)
        {
            throw new InvalidOperationException("O limite do cartao nao pode ser negativo.");
        }

        if (closingDay < 1 || closingDay > 31)
        {
            throw new InvalidOperationException("O dia de fechamento deve estar entre 1 e 31.");
        }

        if (dueDay < 1 || dueDay > 31)
        {
            throw new InvalidOperationException("O dia de vencimento deve estar entre 1 e 31.");
        }

        return new CreditCard
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim(),
            CreditLimit = creditLimit,
            ClosingDay = closingDay,
            DueDay = dueDay,
            IsActive = true,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }
}
namespace FinanceManager.Domain.Entities;

public sealed class CreditCardExpense
{
    private CreditCardExpense()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CreditCardId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Guid TransactionCategoryId { get; private set; }
    public Guid InstallmentGroupId { get; private set; }
    public int InstallmentNumber { get; private set; }
    public int InstallmentCount { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static CreditCardExpense Register(
        Guid userId,
        Guid creditCardId,
        Guid invoiceId,
        Guid transactionCategoryId,
        Guid installmentGroupId,
        int installmentNumber,
        int installmentCount,
        decimal amount,
        DateOnly occurredOn,
        string? description,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("O usuario do lancamento do cartao e obrigatorio.");
        }

        if (creditCardId == Guid.Empty)
        {
            throw new InvalidOperationException("O cartao do lancamento e obrigatorio.");
        }

        if (invoiceId == Guid.Empty)
        {
            throw new InvalidOperationException("A fatura do lancamento e obrigatoria.");
        }

        if (transactionCategoryId == Guid.Empty)
        {
            throw new InvalidOperationException("A categoria do lancamento do cartao e obrigatoria.");
        }

        if (installmentGroupId == Guid.Empty)
        {
            throw new InvalidOperationException("O grupo de parcelamento do lancamento do cartao e obrigatorio.");
        }

        if (installmentCount <= 0)
        {
            throw new InvalidOperationException("A quantidade de parcelas deve ser maior que zero.");
        }

        if (installmentNumber <= 0 || installmentNumber > installmentCount)
        {
            throw new InvalidOperationException("O numero da parcela e invalido.");
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException("O valor do lancamento do cartao deve ser maior que zero.");
        }

        if (occurredOn == default)
        {
            throw new InvalidOperationException("A data do lancamento do cartao e obrigatoria.");
        }

        return new CreditCardExpense
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreditCardId = creditCardId,
            InvoiceId = invoiceId,
            TransactionCategoryId = transactionCategoryId,
            InstallmentGroupId = installmentGroupId,
            InstallmentNumber = installmentNumber,
            InstallmentCount = installmentCount,
            Amount = amount,
            OccurredOn = occurredOn,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }
}

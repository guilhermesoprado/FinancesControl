using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Entities;

public sealed class Transaction
{
    private Transaction()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public string? Description { get; private set; }
    public Guid? FinancialAccountId { get; private set; }
    public Guid? TransactionCategoryId { get; private set; }
    public Guid? SourceFinancialAccountId { get; private set; }
    public Guid? DestinationFinancialAccountId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Transaction CreateIncome(
        Guid userId,
        Guid financialAccountId,
        Guid transactionCategoryId,
        decimal amount,
        DateOnly occurredOn,
        string? description,
        DateTime nowUtc)
    {
        ValidateUser(userId);
        ValidateAmount(amount);
        ValidateOccurredOn(occurredOn);

        if (financialAccountId == Guid.Empty)
        {
            throw new InvalidOperationException("A conta financeira da receita e obrigatoria.");
        }

        if (transactionCategoryId == Guid.Empty)
        {
            throw new InvalidOperationException("A categoria da receita e obrigatoria.");
        }

        return CreateBase(
            userId,
            TransactionType.Income,
            amount,
            occurredOn,
            description,
            nowUtc,
            financialAccountId,
            transactionCategoryId,
            null,
            null);
    }

    public static Transaction CreateExpense(
        Guid userId,
        Guid financialAccountId,
        Guid transactionCategoryId,
        decimal amount,
        DateOnly occurredOn,
        string? description,
        DateTime nowUtc)
    {
        ValidateUser(userId);
        ValidateAmount(amount);
        ValidateOccurredOn(occurredOn);

        if (financialAccountId == Guid.Empty)
        {
            throw new InvalidOperationException("A conta financeira da despesa e obrigatoria.");
        }

        if (transactionCategoryId == Guid.Empty)
        {
            throw new InvalidOperationException("A categoria da despesa e obrigatoria.");
        }

        return CreateBase(
            userId,
            TransactionType.Expense,
            amount,
            occurredOn,
            description,
            nowUtc,
            financialAccountId,
            transactionCategoryId,
            null,
            null);
    }

    public static Transaction CreateTransfer(
        Guid userId,
        Guid sourceFinancialAccountId,
        Guid destinationFinancialAccountId,
        decimal amount,
        DateOnly occurredOn,
        string? description,
        DateTime nowUtc)
    {
        ValidateUser(userId);
        ValidateAmount(amount);
        ValidateOccurredOn(occurredOn);

        if (sourceFinancialAccountId == Guid.Empty)
        {
            throw new InvalidOperationException("A conta de origem da transferencia e obrigatoria.");
        }

        if (destinationFinancialAccountId == Guid.Empty)
        {
            throw new InvalidOperationException("A conta de destino da transferencia e obrigatoria.");
        }

        if (sourceFinancialAccountId == destinationFinancialAccountId)
        {
            throw new InvalidOperationException("A transferencia exige contas de origem e destino diferentes.");
        }

        return CreateBase(
            userId,
            TransactionType.Transfer,
            amount,
            occurredOn,
            description,
            nowUtc,
            null,
            null,
            sourceFinancialAccountId,
            destinationFinancialAccountId);
    }

    private static Transaction CreateBase(
        Guid userId,
        TransactionType type,
        decimal amount,
        DateOnly occurredOn,
        string? description,
        DateTime nowUtc,
        Guid? financialAccountId,
        Guid? transactionCategoryId,
        Guid? sourceFinancialAccountId,
        Guid? destinationFinancialAccountId)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Status = TransactionStatus.Posted,
            Amount = amount,
            OccurredOn = occurredOn,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            FinancialAccountId = financialAccountId,
            TransactionCategoryId = transactionCategoryId,
            SourceFinancialAccountId = sourceFinancialAccountId,
            DestinationFinancialAccountId = destinationFinancialAccountId,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }

    private static void ValidateUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("O usuario da transacao e obrigatorio.");
        }
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("O valor da transacao deve ser maior que zero.");
        }
    }

    private static void ValidateOccurredOn(DateOnly occurredOn)
    {
        if (occurredOn == default)
        {
            throw new InvalidOperationException("A data da transacao e obrigatoria.");
        }
    }
}

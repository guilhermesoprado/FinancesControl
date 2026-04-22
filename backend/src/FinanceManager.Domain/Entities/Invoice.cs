using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Entities;

public sealed class Invoice
{
    private const decimal MinimumPaymentRate = 0.15m;

    private Invoice()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CreditCardId { get; private set; }
    public int ReferenceYear { get; private set; }
    public int ReferenceMonth { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public DateOnly ClosingDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal LateFeeAppliedAmount { get; private set; }
    public decimal LateInterestAppliedAmount { get; private set; }
    public decimal RevolvingInterestAppliedAmount { get; private set; }
    public DateOnly? ChargesAppliedUntilDate { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public Guid? PaidFromFinancialAccountId { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public decimal RemainingAmount => Math.Max(TotalAmount - PaidAmount, 0m);

    public decimal SuggestedMinimumPaymentAmount => RemainingAmount <= 0m
        ? 0m
        : decimal.Round(RemainingAmount * MinimumPaymentRate, 2, MidpointRounding.AwayFromZero);

    public static Invoice Open(
        Guid userId,
        Guid creditCardId,
        int referenceYear,
        int referenceMonth,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly closingDate,
        DateOnly dueDate,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("O usuario da fatura e obrigatorio.");
        }

        if (creditCardId == Guid.Empty)
        {
            throw new InvalidOperationException("O cartao da fatura e obrigatorio.");
        }

        if (referenceMonth < 1 || referenceMonth > 12)
        {
            throw new InvalidOperationException("O mes de referencia da fatura e invalido.");
        }

        if (periodStart > periodEnd)
        {
            throw new InvalidOperationException("O periodo da fatura e invalido.");
        }

        return new Invoice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreditCardId = creditCardId,
            ReferenceYear = referenceYear,
            ReferenceMonth = referenceMonth,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            ClosingDate = closingDate,
            DueDate = dueDate,
            TotalAmount = 0m,
            PaidAmount = 0m,
            LateFeeAppliedAmount = 0m,
            LateInterestAppliedAmount = 0m,
            RevolvingInterestAppliedAmount = 0m,
            Status = InvoiceStatus.Open,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }

    public void Close(DateTime nowUtc)
    {
        if (Status == InvoiceStatus.Paid)
        {
            throw new InvalidOperationException("Nao e possivel fechar uma fatura ja paga.");
        }

        if (Status == InvoiceStatus.Closed)
        {
            throw new InvalidOperationException("A fatura informada ja esta fechada.");
        }

        if (Status == InvoiceStatus.PartiallyPaid)
        {
            throw new InvalidOperationException("Nao e possivel fechar uma fatura parcialmente paga.");
        }

        Status = InvoiceStatus.Closed;
        ClosedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void ApplyPayment(Guid financialAccountId, decimal amount, DateTime nowUtc)
    {
        if (financialAccountId == Guid.Empty)
        {
            throw new InvalidOperationException("A conta financeira de pagamento da fatura e obrigatoria.");
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException("O valor do pagamento deve ser maior que zero.");
        }

        if (Status == InvoiceStatus.Paid)
        {
            throw new InvalidOperationException("A fatura informada ja foi paga.");
        }

        if (amount > RemainingAmount)
        {
            throw new InvalidOperationException("O valor do pagamento nao pode ser maior que o saldo remanescente da fatura.");
        }

        PaidAmount += amount;
        PaidFromFinancialAccountId = financialAccountId;
        PaidAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;

        Status = RemainingAmount == 0m
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartiallyPaid;
    }

    public void MarkAsPaid(Guid financialAccountId, DateTime nowUtc)
    {
        if (financialAccountId == Guid.Empty)
        {
            throw new InvalidOperationException("A conta financeira de pagamento da fatura e obrigatoria.");
        }

        if (Status == InvoiceStatus.Paid)
        {
            throw new InvalidOperationException("A fatura informada ja foi paga.");
        }

        if (RemainingAmount <= 0m)
        {
            PaidFromFinancialAccountId = financialAccountId;
            PaidAtUtc = nowUtc;
            Status = InvoiceStatus.Paid;
            UpdatedAtUtc = nowUtc;
            return;
        }

        ApplyPayment(financialAccountId, RemainingAmount, nowUtc);
    }
    public void AddCharge(decimal amount, DateTime nowUtc, bool allowClosedInvoice = false)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("O valor do lancamento do cartao deve ser maior que zero.");
        }

        if (Status == InvoiceStatus.Paid)
        {
            throw new InvalidOperationException("Nao e possivel adicionar lancamentos a uma fatura ja paga.");
        }

        if ((Status == InvoiceStatus.Closed || Status == InvoiceStatus.PartiallyPaid) && !allowClosedInvoice)
        {
            throw new InvalidOperationException("Nao e possivel adicionar lancamentos a uma fatura fechada sem confirmacao explicita.");
        }

        TotalAmount += amount;
        UpdatedAtUtc = nowUtc;
    }

    public void ApplyAdjustment(InvoiceAdjustmentType adjustmentType, decimal amount, DateTime nowUtc)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("O valor do ajuste deve ser maior que zero.");
        }

        if (Status == InvoiceStatus.Paid)
        {
            throw new InvalidOperationException("Nao e possivel ajustar uma fatura ja paga.");
        }

        var delta = adjustmentType switch
        {
            InvoiceAdjustmentType.Credit => -amount,
            InvoiceAdjustmentType.Discount => -amount,
            InvoiceAdjustmentType.ManualDecrease => -amount,
            InvoiceAdjustmentType.Fee => amount,
            InvoiceAdjustmentType.Interest => amount,
            InvoiceAdjustmentType.Penalty => amount,
            InvoiceAdjustmentType.ManualIncrease => amount,
            _ => throw new InvalidOperationException("O tipo de ajuste informado nao e suportado.")
        };

        var nextTotal = TotalAmount + delta;
        if (nextTotal < PaidAmount)
        {
            throw new InvalidOperationException("O ajuste nao pode reduzir a fatura para menos do que o valor ja pago.");
        }

        TotalAmount = nextTotal;
        UpdatedAtUtc = nowUtc;

        if (RemainingAmount == 0m && PaidAmount > 0m)
        {
            Status = InvoiceStatus.Paid;
            PaidAtUtc ??= nowUtc;
        }
    }

    public void ApplyFinancialCharges(
        DateOnly today,
        DateTime nowUtc,
        decimal penaltyRate,
        decimal lateInterestMonthlyRate,
        decimal revolvingInterestMonthlyRate)
    {
        if (RemainingAmount <= 0m || today <= DueDate)
        {
            return;
        }

        var chargeStartDate = ChargesAppliedUntilDate ?? DueDate;
        if (today <= chargeStartDate)
        {
            return;
        }

        var baseAmount = RemainingAmount;
        var daysOverdue = today.DayNumber - chargeStartDate.DayNumber;
        if (daysOverdue <= 0)
        {
            return;
        }

        var totalCharge = 0m;

        if (LateFeeAppliedAmount == 0m)
        {
            var penalty = decimal.Round(baseAmount * penaltyRate, 2, MidpointRounding.AwayFromZero);
            LateFeeAppliedAmount += penalty;
            totalCharge += penalty;
        }

        var lateInterest = decimal.Round(baseAmount * lateInterestMonthlyRate * daysOverdue / 30m, 2, MidpointRounding.AwayFromZero);
        var revolvingInterest = decimal.Round(baseAmount * revolvingInterestMonthlyRate * daysOverdue / 30m, 2, MidpointRounding.AwayFromZero);

        LateInterestAppliedAmount += lateInterest;
        RevolvingInterestAppliedAmount += revolvingInterest;
        totalCharge += lateInterest + revolvingInterest;

        if (totalCharge > 0m)
        {
            TotalAmount += totalCharge;
            ChargesAppliedUntilDate = today;
            UpdatedAtUtc = nowUtc;
        }
    }
}



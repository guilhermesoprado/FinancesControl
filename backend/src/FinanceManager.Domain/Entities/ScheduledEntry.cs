using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Entities;

public sealed class ScheduledEntry
{
    private ScheduledEntry()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid FinancialAccountId { get; private set; }
    public Guid TransactionCategoryId { get; private set; }
    public TransactionType Type { get; private set; }
    public ScheduledEntryPlanningMode PlanningMode { get; private set; }
    public ScheduledEntryRecurrenceFrequency? RecurrenceFrequency { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? NextOccurrenceDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public ScheduledEntryStatus Status { get; private set; }
    public DateTime? LastRealizedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static ScheduledEntry Create(
        Guid userId,
        Guid financialAccountId,
        Guid transactionCategoryId,
        TransactionType type,
        ScheduledEntryPlanningMode planningMode,
        ScheduledEntryRecurrenceFrequency? recurrenceFrequency,
        decimal amount,
        string? description,
        DateOnly startDate,
        DateOnly? endDate,
        DateTime nowUtc)
    {
        ValidateUser(userId);
        ValidateAccount(financialAccountId);
        ValidateCategory(transactionCategoryId);
        ValidateType(type);
        ValidateAmount(amount);
        ValidateDates(startDate, endDate);
        ValidatePlanning(planningMode, recurrenceFrequency);

        return new ScheduledEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FinancialAccountId = financialAccountId,
            TransactionCategoryId = transactionCategoryId,
            Type = type,
            PlanningMode = planningMode,
            RecurrenceFrequency = recurrenceFrequency,
            Amount = amount,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            StartDate = startDate,
            NextOccurrenceDate = startDate,
            EndDate = endDate,
            Status = ScheduledEntryStatus.Scheduled,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }

    public void MarkAsCompleted(DateTime nowUtc)
    {
        EnsureSchedulable("marcar como realizado");

        LastRealizedAtUtc = nowUtc;

        if (PlanningMode == ScheduledEntryPlanningMode.OneTime)
        {
            Status = ScheduledEntryStatus.Completed;
            NextOccurrenceDate = null;
            UpdatedAtUtc = nowUtc;
            return;
        }

        if (!TryAdvanceOccurrence())
        {
            Status = ScheduledEntryStatus.Completed;
        }
        else
        {
            Status = ScheduledEntryStatus.Scheduled;
        }

        UpdatedAtUtc = nowUtc;
    }

    public void Update(
        Guid financialAccountId,
        Guid transactionCategoryId,
        TransactionType type,
        ScheduledEntryPlanningMode planningMode,
        ScheduledEntryRecurrenceFrequency? recurrenceFrequency,
        decimal amount,
        string? description,
        DateOnly startDate,
        DateOnly? endDate,
        DateTime nowUtc)
    {
        if (Status != ScheduledEntryStatus.Scheduled)
        {
            throw new InvalidOperationException("Somente lancamentos planejados agendados podem ser editados.");
        }

        ValidateAccount(financialAccountId);
        ValidateCategory(transactionCategoryId);
        ValidateType(type);
        ValidateAmount(amount);
        ValidateDates(startDate, endDate);
        ValidatePlanning(planningMode, recurrenceFrequency);

        var currentActiveOccurrence = NextOccurrenceDate;

        FinancialAccountId = financialAccountId;
        TransactionCategoryId = transactionCategoryId;
        Type = type;
        PlanningMode = planningMode;
        RecurrenceFrequency = recurrenceFrequency;
        Amount = amount;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        StartDate = startDate;
        EndDate = endDate;
        NextOccurrenceDate = ResolveNextOccurrenceForUpdate(
            planningMode,
            recurrenceFrequency,
            startDate,
            endDate,
            currentActiveOccurrence);
        UpdatedAtUtc = nowUtc;
    }

    public void Skip(DateTime nowUtc)
    {
        EnsureSchedulable("ignorar");

        if (PlanningMode == ScheduledEntryPlanningMode.OneTime)
        {
            Status = ScheduledEntryStatus.Skipped;
            NextOccurrenceDate = null;
            UpdatedAtUtc = nowUtc;
            return;
        }

        if (!TryAdvanceOccurrence())
        {
            Status = ScheduledEntryStatus.Skipped;
        }
        else
        {
            Status = ScheduledEntryStatus.Scheduled;
        }

        UpdatedAtUtc = nowUtc;
    }

    public void Cancel(DateTime nowUtc)
    {
        if (Status == ScheduledEntryStatus.Cancelled)
        {
            throw new InvalidOperationException("O lancamento planejado informado ja esta cancelado.");
        }

        Status = ScheduledEntryStatus.Cancelled;
        NextOccurrenceDate = null;
        UpdatedAtUtc = nowUtc;
    }

    private bool TryAdvanceOccurrence()
    {
        if (!NextOccurrenceDate.HasValue || !RecurrenceFrequency.HasValue)
        {
            NextOccurrenceDate = null;
            return false;
        }

        var nextOccurrence = RecurrenceFrequency.Value switch
        {
            ScheduledEntryRecurrenceFrequency.Weekly => NextOccurrenceDate.Value.AddDays(7),
            ScheduledEntryRecurrenceFrequency.Monthly => NextOccurrenceDate.Value.AddMonths(1),
            _ => throw new InvalidOperationException("A frequencia do lancamento planejado nao e suportada.")
        };

        if (EndDate.HasValue && nextOccurrence > EndDate.Value)
        {
            NextOccurrenceDate = null;
            return false;
        }

        NextOccurrenceDate = nextOccurrence;
        return true;
    }

    private void EnsureSchedulable(string action)
    {
        if (Status == ScheduledEntryStatus.Cancelled)
        {
            throw new InvalidOperationException($"Nao e possivel {action} um lancamento planejado cancelado.");
        }

        if (Status == ScheduledEntryStatus.Completed)
        {
            throw new InvalidOperationException($"Nao e possivel {action} um lancamento planejado ja concluido.");
        }

        if (Status == ScheduledEntryStatus.Skipped)
        {
            throw new InvalidOperationException($"Nao e possivel {action} um lancamento planejado ja ignorado.");
        }

        if (!NextOccurrenceDate.HasValue)
        {
            throw new InvalidOperationException("O lancamento planejado nao possui proxima ocorrencia ativa.");
        }
    }

    private static void ValidateUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("O usuario do lancamento planejado e obrigatorio.");
        }
    }

    private static void ValidateAccount(Guid financialAccountId)
    {
        if (financialAccountId == Guid.Empty)
        {
            throw new InvalidOperationException("A conta financeira do lancamento planejado e obrigatoria.");
        }
    }

    private static void ValidateCategory(Guid transactionCategoryId)
    {
        if (transactionCategoryId == Guid.Empty)
        {
            throw new InvalidOperationException("A categoria do lancamento planejado e obrigatoria.");
        }
    }

    private static void ValidateType(TransactionType type)
    {
        if (type == TransactionType.Transfer)
        {
            throw new InvalidOperationException("Lancamentos planejados nao suportam o tipo transferencia neste primeiro recorte.");
        }
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new InvalidOperationException("O valor do lancamento planejado deve ser maior que zero.");
        }
    }

    private static void ValidateDates(DateOnly startDate, DateOnly? endDate)
    {
        if (startDate == default)
        {
            throw new InvalidOperationException("A data inicial do lancamento planejado e obrigatoria.");
        }

        if (endDate.HasValue && endDate.Value < startDate)
        {
            throw new InvalidOperationException("A data final do lancamento planejado nao pode ser menor que a data inicial.");
        }
    }

    private static void ValidatePlanning(
        ScheduledEntryPlanningMode planningMode,
        ScheduledEntryRecurrenceFrequency? recurrenceFrequency)
    {
        if (planningMode == ScheduledEntryPlanningMode.OneTime && recurrenceFrequency.HasValue)
        {
            throw new InvalidOperationException("Lancamentos planejados unicos nao podem informar frequencia recorrente.");
        }

        if (planningMode == ScheduledEntryPlanningMode.Recurring && !recurrenceFrequency.HasValue)
        {
            throw new InvalidOperationException("Lancamentos planejados recorrentes exigem frequencia recorrente.");
        }
    }

    private static DateOnly? ResolveNextOccurrenceForUpdate(
        ScheduledEntryPlanningMode planningMode,
        ScheduledEntryRecurrenceFrequency? recurrenceFrequency,
        DateOnly startDate,
        DateOnly? endDate,
        DateOnly? currentActiveOccurrence)
    {
        if (planningMode == ScheduledEntryPlanningMode.OneTime || !recurrenceFrequency.HasValue)
        {
            return startDate;
        }

        var nextOccurrence = currentActiveOccurrence.HasValue && currentActiveOccurrence.Value >= startDate
            ? currentActiveOccurrence.Value
            : startDate;

        if (endDate.HasValue && nextOccurrence > endDate.Value)
        {
            return null;
        }

        return nextOccurrence;
    }
}

using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Entities;

public sealed class ScheduledEntryOccurrence
{
    private ScheduledEntryOccurrence()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ScheduledEntryId { get; private set; }
    public DateOnly OccurrenceDate { get; private set; }
    public ScheduledEntryStatus Status { get; private set; }
    public DateTime TreatedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static ScheduledEntryOccurrence Create(
        Guid userId,
        Guid scheduledEntryId,
        DateOnly occurrenceDate,
        ScheduledEntryStatus status,
        DateTime treatedAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("O usuario da ocorrencia planejada e obrigatorio.");
        }

        if (scheduledEntryId == Guid.Empty)
        {
            throw new InvalidOperationException("O lancamento planejado da ocorrencia e obrigatorio.");
        }

        if (occurrenceDate == default)
        {
            throw new InvalidOperationException("A data da ocorrencia planejada e obrigatoria.");
        }

        if (status == ScheduledEntryStatus.Scheduled)
        {
            throw new InvalidOperationException("Ocorrencias tratadas nao podem ser registradas como agendadas.");
        }

        return new ScheduledEntryOccurrence
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ScheduledEntryId = scheduledEntryId,
            OccurrenceDate = occurrenceDate,
            Status = status,
            TreatedAtUtc = treatedAtUtc,
            CreatedAtUtc = treatedAtUtc,
        };
    }
}

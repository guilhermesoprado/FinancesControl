using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.ScheduledEntries.Contracts;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.ScheduledEntries.Services;

public sealed class ScheduledEntryService : IScheduledEntryService
{
    private const string PlannedEntryTransactionPrefix = "Planejamento";

    private readonly IScheduledEntryRepository _scheduledEntryRepository;
    private readonly IScheduledEntryOccurrenceRepository _scheduledEntryOccurrenceRepository;
    private readonly IFinancialAccountRepository _financialAccountRepository;
    private readonly ITransactionCategoryRepository _transactionCategoryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ScheduledEntryService(
        IScheduledEntryRepository scheduledEntryRepository,
        IScheduledEntryOccurrenceRepository scheduledEntryOccurrenceRepository,
        IFinancialAccountRepository financialAccountRepository,
        ITransactionCategoryRepository transactionCategoryRepository,
        ITransactionRepository transactionRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _scheduledEntryRepository = scheduledEntryRepository;
        _scheduledEntryOccurrenceRepository = scheduledEntryOccurrenceRepository;
        _financialAccountRepository = financialAccountRepository;
        _transactionCategoryRepository = transactionCategoryRepository;
        _transactionRepository = transactionRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ScheduledEntryDto> CreateAsync(CreateScheduledEntryInput input, CancellationToken cancellationToken)
    {
        ValidateCreateInput(input);

        var financialAccount = await RequireFinancialAccountAsync(input.UserId, input.FinancialAccountId, cancellationToken);
        var category = await RequireCategoryAsync(input.UserId, input.TransactionCategoryId, cancellationToken);
        var nowUtc = _dateTimeProvider.UtcNow;

        ScheduledEntry scheduledEntry;

        try
        {
            scheduledEntry = ScheduledEntry.Create(
                input.UserId,
                financialAccount.Id,
                category.Id,
                MapTransactionType(category.Type),
                input.PlanningMode,
                input.RecurrenceFrequency,
                input.Amount,
                input.Description,
                input.StartDate,
                input.EndDate,
                nowUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new AppValidationException(exception.Message);
        }

        await _scheduledEntryRepository.AddAsync(scheduledEntry, cancellationToken);
        await _scheduledEntryRepository.SaveChangesAsync(cancellationToken);

        return Map(scheduledEntry, financialAccount.Name, category.Name);
    }

    public async Task<ScheduledEntryDto> UpdateAsync(UpdateScheduledEntryInput input, CancellationToken cancellationToken)
    {
        ValidateUpdateInput(input);

        var scheduledEntry = await _scheduledEntryRepository.GetByUserAndIdAsync(input.UserId, input.ScheduledEntryId, cancellationToken);
        if (scheduledEntry is null)
        {
            throw new AppValidationException("O lancamento planejado informado nao foi encontrado.");
        }

        var financialAccount = await RequireFinancialAccountAsync(input.UserId, input.FinancialAccountId, cancellationToken);
        var category = await RequireCategoryAsync(input.UserId, input.TransactionCategoryId, cancellationToken);
        var nowUtc = _dateTimeProvider.UtcNow;

        try
        {
            scheduledEntry.Update(
                financialAccount.Id,
                category.Id,
                MapTransactionType(category.Type),
                input.PlanningMode,
                input.RecurrenceFrequency,
                input.Amount,
                input.Description,
                input.StartDate,
                input.EndDate,
                nowUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new AppValidationException(exception.Message);
        }

        await _scheduledEntryRepository.SaveChangesAsync(cancellationToken);

        return Map(scheduledEntry, financialAccount.Name, category.Name);
    }

    public async Task<IReadOnlyList<ScheduledEntryOccurrenceDto>> GetByUserAsync(GetScheduledEntriesInput input, CancellationToken cancellationToken)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para listar lancamentos planejados.");
        }

        if (input.From.HasValue && input.To.HasValue && input.From.Value > input.To.Value)
        {
            throw new AppValidationException("A data inicial do filtro nao pode ser maior que a data final.");
        }

        var scheduledEntries = await _scheduledEntryRepository.GetByUserAsync(
            input.UserId,
            null,
            null,
            null,
            cancellationToken);
        var loggedOccurrences = await _scheduledEntryOccurrenceRepository.GetByUserAsync(
            input.UserId,
            input.Status == ScheduledEntryStatus.Scheduled ? null : input.Status,
            input.From,
            input.To,
            cancellationToken);

        var financialAccounts = await _financialAccountRepository.GetByUserIdAsync(input.UserId, cancellationToken);
        var categories = await _transactionCategoryRepository.GetByUserIdAsync(input.UserId, cancellationToken);
        var financialAccountMap = financialAccounts.ToDictionary(x => x.Id, x => x.Name);
        var categoryMap = categories.ToDictionary(x => x.Id, x => x.Name);
        var occurrenceMap = loggedOccurrences
            .GroupBy(x => x.ScheduledEntryId)
            .ToDictionary(group => group.Key, group => (IReadOnlyCollection<ScheduledEntryOccurrence>)group.ToList());
        var visibleOccurrences = new List<ScheduledEntryOccurrenceDto>();

        foreach (var entry in scheduledEntries)
        {
            var accountName = financialAccountMap.TryGetValue(entry.FinancialAccountId, out var currentAccountName)
                ? currentAccountName
                : "Conta nao encontrada";
            var categoryName = categoryMap.TryGetValue(entry.TransactionCategoryId, out var currentCategoryName)
                ? currentCategoryName
                : "Categoria nao encontrada";
            var entryOccurrences = occurrenceMap.TryGetValue(entry.Id, out var currentOccurrences)
                ? currentOccurrences
                : Array.Empty<ScheduledEntryOccurrence>();

            visibleOccurrences.AddRange(MapLoggedOccurrences(entry, entryOccurrences, accountName, categoryName));
            visibleOccurrences.AddRange(MapScheduledOccurrences(entry, input.From, input.To, accountName, categoryName));
            visibleOccurrences.AddRange(MapFallbackOccurrences(entry, entryOccurrences, accountName, categoryName));
        }

        return visibleOccurrences
            .Where(x => !input.Status.HasValue || x.Status == input.Status.Value)
            .Where(x => !input.From.HasValue || x.OccurrenceDate >= input.From.Value)
            .Where(x => !input.To.HasValue || x.OccurrenceDate <= input.To.Value)
            .OrderBy(x => x.OccurrenceDate)
            .ThenBy(x => x.ScheduledEntryId)
            .ThenBy(x => x.OccurrenceKey)
            .ToList();
    }

    public Task<ScheduledEntryDto> CompleteAsync(ApplyScheduledEntryOccurrenceActionInput input, CancellationToken cancellationToken)
    {
        return ExecuteStateChangeAsync(
            input,
            "marcar lancamento planejado como realizado",
            ScheduledEntryStatus.Completed,
            static (entry, nowUtc) => entry.MarkAsCompleted(nowUtc),
            cancellationToken);
    }

    public async Task<ScheduledEntryDto> UndoCompleteAsync(ApplyScheduledEntryOccurrenceActionInput input, CancellationToken cancellationToken)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para desfazer o realizado.");
        }

        if (input.ScheduledEntryId == Guid.Empty)
        {
            throw new AppValidationException("O lancamento planejado informado e obrigatorio.");
        }

        if (input.OccurrenceDate == default)
        {
            throw new AppValidationException("A competencia informada para desfazer o realizado e obrigatoria.");
        }

        var scheduledEntry = await _scheduledEntryRepository.GetByUserAndIdAsync(input.UserId, input.ScheduledEntryId, cancellationToken);
        if (scheduledEntry is null)
        {
            throw new AppValidationException("O lancamento planejado informado nao foi encontrado.");
        }

        var occurrence = await _scheduledEntryOccurrenceRepository.GetByUserScheduledEntryAndDateAsync(
            input.UserId,
            input.ScheduledEntryId,
            input.OccurrenceDate,
            cancellationToken);

        if (occurrence is null || occurrence.Status != ScheduledEntryStatus.Completed)
        {
            throw new AppValidationException("Nao existe um realizado ativo para a competencia informada.");
        }

        var financialAccount = await RequireFinancialAccountAsync(input.UserId, scheduledEntry.FinancialAccountId, cancellationToken);
        var category = await RequireCategoryAsync(input.UserId, scheduledEntry.TransactionCategoryId, cancellationToken);
        var nowUtc = _dateTimeProvider.UtcNow;

        var linkedTransaction = await FindPlannedEntryTransactionAsync(
            input.UserId,
            scheduledEntry,
            input.OccurrenceDate,
            cancellationToken);

        if (linkedTransaction is not null)
        {
            if (linkedTransaction.Type == TransactionType.Income)
            {
                financialAccount.ReconcileDelta(-linkedTransaction.Amount, nowUtc);
            }
            else
            {
                financialAccount.ReconcileDelta(linkedTransaction.Amount, nowUtc);
            }

            _transactionRepository.Remove(linkedTransaction);
        }

        scheduledEntry.UndoCompletion(input.OccurrenceDate, nowUtc);
        _scheduledEntryOccurrenceRepository.Remove(occurrence);

        await _scheduledEntryRepository.SaveChangesAsync(cancellationToken);

        return Map(scheduledEntry, financialAccount.Name, category.Name);
    }

    public Task<ScheduledEntryDto> SkipAsync(ApplyScheduledEntryOccurrenceActionInput input, CancellationToken cancellationToken)
    {
        return ExecuteStateChangeAsync(
            input,
            "ignorar lancamento planejado",
            ScheduledEntryStatus.Skipped,
            static (entry, nowUtc) => entry.Skip(nowUtc),
            cancellationToken);
    }

    public Task<ScheduledEntryDto> CancelAsync(ApplyScheduledEntryOccurrenceActionInput input, CancellationToken cancellationToken)
    {
        return ExecuteStateChangeAsync(
            input,
            "cancelar lancamento planejado",
            ScheduledEntryStatus.Cancelled,
            static (entry, nowUtc) => entry.Cancel(nowUtc),
            cancellationToken);
    }

    private async Task<ScheduledEntryDto> ExecuteStateChangeAsync(
        ApplyScheduledEntryOccurrenceActionInput input,
        string actionDescription,
        ScheduledEntryStatus occurrenceStatus,
        Action<ScheduledEntry, DateTime> action,
        CancellationToken cancellationToken)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException($"Usuario autenticado nao encontrado para {actionDescription}.");
        }

        if (input.ScheduledEntryId == Guid.Empty)
        {
            throw new AppValidationException("O lancamento planejado informado e obrigatorio.");
        }

        if (input.OccurrenceDate == default)
        {
            throw new AppValidationException("A competencia informada para tratar o lancamento planejado e obrigatoria.");
        }

        var scheduledEntry = await _scheduledEntryRepository.GetByUserAndIdAsync(input.UserId, input.ScheduledEntryId, cancellationToken);
        if (scheduledEntry is null)
        {
            throw new AppValidationException("O lancamento planejado informado nao foi encontrado.");
        }

        var activeOccurrenceDate = scheduledEntry.NextOccurrenceDate ?? scheduledEntry.StartDate;
        if (activeOccurrenceDate != input.OccurrenceDate)
        {
            throw new AppValidationException("A recorrencia foi alterada e a competencia selecionada nao esta mais ativa. Recarregue a lista antes de continuar.");
        }

        var financialAccount = await RequireFinancialAccountAsync(input.UserId, scheduledEntry.FinancialAccountId, cancellationToken);
        var category = await RequireCategoryAsync(input.UserId, scheduledEntry.TransactionCategoryId, cancellationToken);
        var nowUtc = _dateTimeProvider.UtcNow;

        try
        {
            action(scheduledEntry, nowUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new AppValidationException(exception.Message);
        }

        var occurrence = ScheduledEntryOccurrence.Create(
            input.UserId,
            scheduledEntry.Id,
            input.OccurrenceDate,
            occurrenceStatus,
            nowUtc);
        await _scheduledEntryOccurrenceRepository.AddAsync(occurrence, cancellationToken);

        if (occurrenceStatus == ScheduledEntryStatus.Completed)
        {
            var transaction = CreateTransactionFromScheduledEntry(
                scheduledEntry,
                input.OccurrenceDate,
                nowUtc);

            if (transaction.Type == TransactionType.Income)
            {
                financialAccount.ApplyDelta(transaction.Amount, nowUtc);
            }
            else
            {
                financialAccount.ApplyDelta(-transaction.Amount, nowUtc);
            }

            await _transactionRepository.AddAsync(transaction, cancellationToken);
        }

        await _scheduledEntryRepository.SaveChangesAsync(cancellationToken);

        return Map(scheduledEntry, financialAccount.Name, category.Name);
    }

    private Transaction CreateTransactionFromScheduledEntry(
        ScheduledEntry scheduledEntry,
        DateOnly occurrenceDate,
        DateTime nowUtc)
    {
        var description = BuildPlannedEntryTransactionDescription(
            scheduledEntry.Description,
            scheduledEntry.TransactionCategoryId,
            occurrenceDate);

        return scheduledEntry.Type == TransactionType.Income
            ? Transaction.CreateIncome(
                scheduledEntry.UserId,
                scheduledEntry.FinancialAccountId,
                scheduledEntry.TransactionCategoryId,
                scheduledEntry.Amount,
                occurrenceDate,
                description,
                nowUtc)
            : Transaction.CreateExpense(
                scheduledEntry.UserId,
                scheduledEntry.FinancialAccountId,
                scheduledEntry.TransactionCategoryId,
                scheduledEntry.Amount,
                occurrenceDate,
                description,
                nowUtc);
    }

    private async Task<Transaction?> FindPlannedEntryTransactionAsync(
        Guid userId,
        ScheduledEntry scheduledEntry,
        DateOnly occurrenceDate,
        CancellationToken cancellationToken)
    {
        var expectedDescription = BuildPlannedEntryTransactionDescription(
            scheduledEntry.Description,
            scheduledEntry.TransactionCategoryId,
            occurrenceDate);

        var transactions = await _transactionRepository.GetByUserAndPeriodAsync(
            userId,
            occurrenceDate,
            occurrenceDate,
            scheduledEntry.Type,
            scheduledEntry.FinancialAccountId,
            cancellationToken);

        return transactions
            .Where(transaction => transaction.TransactionCategoryId == scheduledEntry.TransactionCategoryId)
            .Where(transaction => transaction.Amount == scheduledEntry.Amount)
            .Where(transaction => transaction.Description == expectedDescription)
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .FirstOrDefault();
    }

    private static string BuildPlannedEntryTransactionDescription(
        string? baseDescription,
        Guid transactionCategoryId,
        DateOnly occurrenceDate)
    {
        var normalizedBaseDescription = string.IsNullOrWhiteSpace(baseDescription)
            ? "Lancamento previsto"
            : baseDescription.Trim();

        return $"{PlannedEntryTransactionPrefix}: {normalizedBaseDescription} | {occurrenceDate:yyyy-MM-dd} | {transactionCategoryId:D}";
    }

    private async Task<FinancialAccount> RequireFinancialAccountAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken)
    {
        var financialAccount = await _financialAccountRepository.GetByUserIdAndIdAsync(userId, financialAccountId, cancellationToken);
        if (financialAccount is null)
        {
            throw new AppValidationException("A conta financeira informada nao foi encontrada para o usuario autenticado.");
        }

        if (!financialAccount.IsActive)
        {
            throw new AppValidationException("A conta financeira informada esta inativa.");
        }

        return financialAccount;
    }

    private async Task<TransactionCategory> RequireCategoryAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken)
    {
        var category = await _transactionCategoryRepository.GetByUserIdAndIdAsync(userId, transactionCategoryId, cancellationToken);
        if (category is null)
        {
            throw new AppValidationException("A categoria informada nao foi encontrada para o usuario autenticado.");
        }

        if (!category.IsActive)
        {
            throw new AppValidationException("A categoria informada esta inativa.");
        }

        if (category.Type != TransactionCategoryType.Income && category.Type != TransactionCategoryType.Expense)
        {
            throw new AppValidationException("A categoria informada nao e compativel com lancamentos planejados neste recorte.");
        }

        return category;
    }

    private static TransactionType MapTransactionType(TransactionCategoryType categoryType)
    {
        return categoryType switch
        {
            TransactionCategoryType.Income => TransactionType.Income,
            TransactionCategoryType.Expense => TransactionType.Expense,
            _ => throw new AppValidationException("A categoria informada nao e compativel com lancamentos planejados neste recorte.")
        };
    }

    private static ScheduledEntryDto Map(ScheduledEntry scheduledEntry, string financialAccountName, string transactionCategoryName)
    {
        return new ScheduledEntryDto(
            scheduledEntry.Id,
            scheduledEntry.FinancialAccountId,
            financialAccountName,
            scheduledEntry.TransactionCategoryId,
            transactionCategoryName,
            scheduledEntry.Type,
            scheduledEntry.PlanningMode,
            scheduledEntry.RecurrenceFrequency,
            scheduledEntry.Amount,
            scheduledEntry.Description,
            scheduledEntry.StartDate,
            scheduledEntry.NextOccurrenceDate,
            scheduledEntry.EndDate,
            scheduledEntry.Status,
            scheduledEntry.LastRealizedAtUtc,
            scheduledEntry.CreatedAtUtc);
    }

    private static IEnumerable<ScheduledEntryOccurrenceDto> MapLoggedOccurrences(
        ScheduledEntry scheduledEntry,
        IReadOnlyCollection<ScheduledEntryOccurrence> occurrences,
        string financialAccountName,
        string transactionCategoryName)
    {
        return occurrences.Select(occurrence => new ScheduledEntryOccurrenceDto(
            occurrence.Id.ToString(),
            scheduledEntry.Id,
            scheduledEntry.FinancialAccountId,
            financialAccountName,
            scheduledEntry.TransactionCategoryId,
            transactionCategoryName,
            scheduledEntry.Type,
            scheduledEntry.PlanningMode,
            scheduledEntry.RecurrenceFrequency,
            scheduledEntry.Amount,
            scheduledEntry.Description,
            scheduledEntry.StartDate,
            occurrence.OccurrenceDate,
            scheduledEntry.NextOccurrenceDate,
            scheduledEntry.EndDate,
            occurrence.Status,
            occurrence.TreatedAtUtc,
            false,
            false,
            occurrence.CreatedAtUtc));
    }

    private static IEnumerable<ScheduledEntryOccurrenceDto> MapScheduledOccurrences(
        ScheduledEntry scheduledEntry,
        DateOnly? from,
        DateOnly? to,
        string financialAccountName,
        string transactionCategoryName)
    {
        if (scheduledEntry.Status != ScheduledEntryStatus.Scheduled || !scheduledEntry.NextOccurrenceDate.HasValue)
        {
            return [];
        }

        var visibleOccurrences = new List<ScheduledEntryOccurrenceDto>();
        var currentOccurrenceDate = scheduledEntry.NextOccurrenceDate.Value;
        var currentDate = currentOccurrenceDate;
        var effectiveTo = to ?? currentOccurrenceDate;

        while (currentDate <= effectiveTo && (!scheduledEntry.EndDate.HasValue || currentDate <= scheduledEntry.EndDate.Value))
        {
            visibleOccurrences.Add(new ScheduledEntryOccurrenceDto(
                $"{scheduledEntry.Id}:{currentDate:yyyy-MM-dd}:scheduled",
                scheduledEntry.Id,
                scheduledEntry.FinancialAccountId,
                financialAccountName,
                scheduledEntry.TransactionCategoryId,
                transactionCategoryName,
                scheduledEntry.Type,
                scheduledEntry.PlanningMode,
                scheduledEntry.RecurrenceFrequency,
                scheduledEntry.Amount,
                scheduledEntry.Description,
                scheduledEntry.StartDate,
                currentDate,
                scheduledEntry.NextOccurrenceDate,
                scheduledEntry.EndDate,
                ScheduledEntryStatus.Scheduled,
                null,
                currentDate == currentOccurrenceDate,
                currentDate == currentOccurrenceDate,
                scheduledEntry.CreatedAtUtc));

            if (scheduledEntry.PlanningMode != ScheduledEntryPlanningMode.Recurring || !scheduledEntry.RecurrenceFrequency.HasValue)
            {
                break;
            }

            currentDate = AdvanceOccurrence(currentDate, scheduledEntry.RecurrenceFrequency.Value);
        }

        return visibleOccurrences
            .Where(x => !from.HasValue || x.OccurrenceDate >= from.Value);
    }

    private static IEnumerable<ScheduledEntryOccurrenceDto> MapFallbackOccurrences(
        ScheduledEntry scheduledEntry,
        IReadOnlyCollection<ScheduledEntryOccurrence> occurrences,
        string financialAccountName,
        string transactionCategoryName)
    {
        if (scheduledEntry.Status == ScheduledEntryStatus.Scheduled)
        {
            return MapLegacyCompletedRecurringOccurrence(scheduledEntry, occurrences, financialAccountName, transactionCategoryName);
        }

        if (occurrences.Count > 0)
        {
            return [];
        }

        var fallbackDate = ResolveFallbackOccurrenceDate(scheduledEntry);
        return
        [
            new ScheduledEntryOccurrenceDto(
                $"{scheduledEntry.Id}:{fallbackDate:yyyy-MM-dd}:{scheduledEntry.Status.ToString().ToLowerInvariant()}:fallback",
                scheduledEntry.Id,
                scheduledEntry.FinancialAccountId,
                financialAccountName,
                scheduledEntry.TransactionCategoryId,
                transactionCategoryName,
                scheduledEntry.Type,
                scheduledEntry.PlanningMode,
                scheduledEntry.RecurrenceFrequency,
                scheduledEntry.Amount,
                scheduledEntry.Description,
                scheduledEntry.StartDate,
                fallbackDate,
                scheduledEntry.NextOccurrenceDate,
                scheduledEntry.EndDate,
                scheduledEntry.Status,
                scheduledEntry.LastRealizedAtUtc ?? scheduledEntry.UpdatedAtUtc,
                false,
                false,
                scheduledEntry.CreatedAtUtc),
        ];
    }

    private static IEnumerable<ScheduledEntryOccurrenceDto> MapLegacyCompletedRecurringOccurrence(
        ScheduledEntry scheduledEntry,
        IReadOnlyCollection<ScheduledEntryOccurrence> occurrences,
        string financialAccountName,
        string transactionCategoryName)
    {
        if (scheduledEntry.PlanningMode != ScheduledEntryPlanningMode.Recurring
            || !scheduledEntry.RecurrenceFrequency.HasValue
            || !scheduledEntry.NextOccurrenceDate.HasValue
            || !scheduledEntry.LastRealizedAtUtc.HasValue)
        {
            return [];
        }

        var completedOccurrenceDate = PreviousOccurrence(scheduledEntry.NextOccurrenceDate.Value, scheduledEntry.RecurrenceFrequency.Value);
        if (completedOccurrenceDate < scheduledEntry.StartDate || occurrences.Any(x => x.OccurrenceDate == completedOccurrenceDate))
        {
            return [];
        }

        return
        [
            new ScheduledEntryOccurrenceDto(
                $"{scheduledEntry.Id}:{completedOccurrenceDate:yyyy-MM-dd}:completed:legacy",
                scheduledEntry.Id,
                scheduledEntry.FinancialAccountId,
                financialAccountName,
                scheduledEntry.TransactionCategoryId,
                transactionCategoryName,
                scheduledEntry.Type,
                scheduledEntry.PlanningMode,
                scheduledEntry.RecurrenceFrequency,
                scheduledEntry.Amount,
                scheduledEntry.Description,
                scheduledEntry.StartDate,
                completedOccurrenceDate,
                scheduledEntry.NextOccurrenceDate,
                scheduledEntry.EndDate,
                ScheduledEntryStatus.Completed,
                scheduledEntry.LastRealizedAtUtc,
                false,
                false,
                scheduledEntry.CreatedAtUtc),
        ];
    }

    private static DateOnly ResolveFallbackOccurrenceDate(ScheduledEntry scheduledEntry)
    {
        if (scheduledEntry.PlanningMode == ScheduledEntryPlanningMode.Recurring
            && scheduledEntry.RecurrenceFrequency.HasValue
            && scheduledEntry.NextOccurrenceDate.HasValue
            && scheduledEntry.Status == ScheduledEntryStatus.Completed)
        {
            return PreviousOccurrence(scheduledEntry.NextOccurrenceDate.Value, scheduledEntry.RecurrenceFrequency.Value);
        }

        return scheduledEntry.StartDate;
    }

    private static DateOnly AdvanceOccurrence(DateOnly currentDate, ScheduledEntryRecurrenceFrequency recurrenceFrequency)
    {
        return recurrenceFrequency switch
        {
            ScheduledEntryRecurrenceFrequency.Weekly => currentDate.AddDays(7),
            ScheduledEntryRecurrenceFrequency.Monthly => currentDate.AddMonths(1),
            _ => throw new AppValidationException("A frequencia recorrente informada nao e suportada.")
        };
    }

    private static DateOnly PreviousOccurrence(DateOnly currentDate, ScheduledEntryRecurrenceFrequency recurrenceFrequency)
    {
        return recurrenceFrequency switch
        {
            ScheduledEntryRecurrenceFrequency.Weekly => currentDate.AddDays(-7),
            ScheduledEntryRecurrenceFrequency.Monthly => currentDate.AddMonths(-1),
            _ => throw new AppValidationException("A frequencia recorrente informada nao e suportada.")
        };
    }

    private static void ValidateCreateInput(CreateScheduledEntryInput input)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para criar lancamento planejado.");
        }

        if (input.FinancialAccountId == Guid.Empty)
        {
            throw new AppValidationException("A conta financeira do lancamento planejado e obrigatoria.");
        }

        if (input.TransactionCategoryId == Guid.Empty)
        {
            throw new AppValidationException("A categoria do lancamento planejado e obrigatoria.");
        }

        if (input.Amount <= 0m)
        {
            throw new AppValidationException("O valor do lancamento planejado deve ser maior que zero.");
        }

        if (input.StartDate == default)
        {
            throw new AppValidationException("A data inicial do lancamento planejado e obrigatoria.");
        }

        if (input.EndDate.HasValue && input.EndDate.Value < input.StartDate)
        {
            throw new AppValidationException("A data final do lancamento planejado nao pode ser menor que a data inicial.");
        }

        if (input.PlanningMode == ScheduledEntryPlanningMode.OneTime && input.RecurrenceFrequency.HasValue)
        {
            throw new AppValidationException("Lancamentos planejados unicos nao podem informar frequencia recorrente.");
        }

        if (input.PlanningMode == ScheduledEntryPlanningMode.Recurring && !input.RecurrenceFrequency.HasValue)
        {
            throw new AppValidationException("Lancamentos planejados recorrentes exigem frequencia recorrente.");
        }
    }

    private static void ValidateUpdateInput(UpdateScheduledEntryInput input)
    {
        if (input.ScheduledEntryId == Guid.Empty)
        {
            throw new AppValidationException("O lancamento planejado informado e obrigatorio.");
        }

        ValidateCreateInput(new CreateScheduledEntryInput(
            input.UserId,
            input.FinancialAccountId,
            input.TransactionCategoryId,
            input.PlanningMode,
            input.RecurrenceFrequency,
            input.Amount,
            input.Description,
            input.StartDate,
            input.EndDate));
    }
}

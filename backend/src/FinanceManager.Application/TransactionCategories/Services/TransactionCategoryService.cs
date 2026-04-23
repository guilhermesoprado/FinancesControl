using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.TransactionCategories.Contracts;
using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.TransactionCategories.Services;

public sealed class TransactionCategoryService : ITransactionCategoryService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IScheduledEntryRepository _scheduledEntryRepository;
    private readonly ITransactionCategoryRepository _transactionCategoryRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TransactionCategoryService(
        IAuditLogRepository auditLogRepository,
        IScheduledEntryRepository scheduledEntryRepository,
        ITransactionCategoryRepository transactionCategoryRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _auditLogRepository = auditLogRepository;
        _scheduledEntryRepository = scheduledEntryRepository;
        _transactionCategoryRepository = transactionCategoryRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<TransactionCategoryDto> CreateAsync(CreateTransactionCategoryInput input, CancellationToken cancellationToken)
    {
        ValidateCreateInput(input);

        var normalizedName = input.Name.Trim().ToUpperInvariant();
        var alreadyExists = await _transactionCategoryRepository.ExistsByUserAndNameAndTypeAsync(
            input.UserId,
            normalizedName,
            input.Type,
            cancellationToken);

        if (alreadyExists)
        {
            throw new AppValidationException("Ja existe uma categoria com o mesmo nome e tipo para este usuario.");
        }

        var transactionCategory = TransactionCategory.CreateUserCategory(
            input.UserId,
            input.Name,
            input.Type,
            input.Color,
            input.Icon,
            _dateTimeProvider.UtcNow);

        await _transactionCategoryRepository.AddAsync(transactionCategory, cancellationToken);
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                input.UserId,
                Domain.Enums.AuditLogEntityType.TransactionCategory,
                transactionCategory.Id,
                Domain.Enums.AuditLogAction.Created,
                BuildCreateSummary(transactionCategory),
                _dateTimeProvider.UtcNow),
            cancellationToken);
        await _transactionCategoryRepository.SaveChangesAsync(cancellationToken);

        return Map(transactionCategory);
    }

    public async Task<TransactionCategoryDto> UpdateAsync(UpdateTransactionCategoryInput input, CancellationToken cancellationToken)
    {
        ValidateUpdateInput(input);

        var transactionCategory = await _transactionCategoryRepository.GetByUserIdAndIdAsync(
            input.UserId,
            input.TransactionCategoryId,
            cancellationToken);

        if (transactionCategory is null)
        {
            throw new AppValidationException("A categoria informada nao foi encontrada para o usuario autenticado.");
        }

        var normalizedName = input.Name.Trim().ToUpperInvariant();
        var alreadyExists = await _transactionCategoryRepository.ExistsByUserAndNameAndTypeAsync(
            input.UserId,
            normalizedName,
            transactionCategory.Type,
            cancellationToken);

        if (alreadyExists && !string.Equals(transactionCategory.Name, input.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new AppValidationException("Ja existe uma categoria com o mesmo nome e tipo para este usuario.");
        }

        var previousName = transactionCategory.Name;
        var previousColor = transactionCategory.Color;
        var previousIcon = transactionCategory.Icon;

        try
        {
            transactionCategory.Update(
                input.Name,
                input.Color,
                input.Icon,
                _dateTimeProvider.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new AppValidationException(exception.Message);
        }

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                input.UserId,
                Domain.Enums.AuditLogEntityType.TransactionCategory,
                transactionCategory.Id,
                Domain.Enums.AuditLogAction.Updated,
                BuildUpdateSummary(previousName, previousColor, previousIcon, transactionCategory),
                _dateTimeProvider.UtcNow),
            cancellationToken);
        await _transactionCategoryRepository.SaveChangesAsync(cancellationToken);
        return Map(transactionCategory);
    }

    public async Task<TransactionCategoryDto> InactivateAsync(InactivateTransactionCategoryInput input, CancellationToken cancellationToken)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para inativar categoria.");
        }

        if (input.TransactionCategoryId == Guid.Empty)
        {
            throw new AppValidationException("A categoria informada e obrigatoria.");
        }

        var transactionCategory = await _transactionCategoryRepository.GetByUserIdAndIdAsync(
            input.UserId,
            input.TransactionCategoryId,
            cancellationToken);

        if (transactionCategory is null)
        {
            throw new AppValidationException("A categoria informada nao foi encontrada para o usuario autenticado.");
        }

        var hasActiveScheduledEntries = await _scheduledEntryRepository.ExistsActiveByUserAndTransactionCategoryIdAsync(
            input.UserId,
            transactionCategory.Id,
            cancellationToken);

        if (hasActiveScheduledEntries)
        {
            throw new AppValidationException("Nao e possivel inativar uma categoria vinculada a lancamentos planejados ativos.");
        }

        try
        {
            transactionCategory.Inactivate(_dateTimeProvider.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new AppValidationException(exception.Message);
        }

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                input.UserId,
                Domain.Enums.AuditLogEntityType.TransactionCategory,
                transactionCategory.Id,
                Domain.Enums.AuditLogAction.Inactivated,
                BuildInactivationSummary(transactionCategory),
                _dateTimeProvider.UtcNow),
            cancellationToken);
        await _transactionCategoryRepository.SaveChangesAsync(cancellationToken);
        return Map(transactionCategory);
    }

    public async Task<IReadOnlyList<TransactionCategoryDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para listar categorias.");
        }

        var categories = await _transactionCategoryRepository.GetByUserIdAsync(userId, cancellationToken);
        return categories.Select(Map).ToList();
    }

    private static TransactionCategoryDto Map(TransactionCategory transactionCategory)
    {
        return new TransactionCategoryDto(
            transactionCategory.Id,
            transactionCategory.Name,
            transactionCategory.Type,
            transactionCategory.Color,
            transactionCategory.Icon,
            transactionCategory.IsSystem,
            transactionCategory.IsActive,
            transactionCategory.CreatedAtUtc);
    }

    private static void ValidateCreateInput(CreateTransactionCategoryInput input)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para criar categoria.");
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new AppValidationException("O nome da categoria e obrigatorio.");
        }
    }

    private static void ValidateUpdateInput(UpdateTransactionCategoryInput input)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para editar categoria.");
        }

        if (input.TransactionCategoryId == Guid.Empty)
        {
            throw new AppValidationException("A categoria informada e obrigatoria.");
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new AppValidationException("O nome da categoria e obrigatorio.");
        }
    }

    private static string BuildCreateSummary(TransactionCategory transactionCategory)
    {
        return $"Categoria criada: nome='{transactionCategory.Name}', tipo='{transactionCategory.Type}', cor='{transactionCategory.Color ?? "-"}', icone='{transactionCategory.Icon ?? "-"}'.";
    }

    private static string BuildUpdateSummary(
        string previousName,
        string? previousColor,
        string? previousIcon,
        TransactionCategory transactionCategory)
    {
        return $"Categoria atualizada: nome='{previousName}' -> '{transactionCategory.Name}', cor='{previousColor ?? "-"}' -> '{transactionCategory.Color ?? "-"}', icone='{previousIcon ?? "-"}' -> '{transactionCategory.Icon ?? "-"}'.";
    }

    private static string BuildInactivationSummary(TransactionCategory transactionCategory)
    {
        return $"Categoria inativada: nome='{transactionCategory.Name}', tipo='{transactionCategory.Type}'.";
    }
}

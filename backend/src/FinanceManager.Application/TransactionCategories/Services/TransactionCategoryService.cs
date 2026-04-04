using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.TransactionCategories.Contracts;
using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.TransactionCategories.Services;

public sealed class TransactionCategoryService : ITransactionCategoryService
{
    private readonly ITransactionCategoryRepository _transactionCategoryRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TransactionCategoryService(
        ITransactionCategoryRepository transactionCategoryRepository,
        IDateTimeProvider dateTimeProvider)
    {
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
}

using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.Transactions.Contracts;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Transactions.Services;

public sealed class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFinancialAccountRepository _financialAccountRepository;
    private readonly ITransactionCategoryRepository _transactionCategoryRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IFinancialAccountRepository financialAccountRepository,
        ITransactionCategoryRepository transactionCategoryRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _transactionRepository = transactionRepository;
        _financialAccountRepository = financialAccountRepository;
        _transactionCategoryRepository = transactionCategoryRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<TransactionDto> RegisterIncomeAsync(CreateIncomeTransactionInput input, CancellationToken cancellationToken)
    {
        ValidateCommonInput(input.UserId, input.Amount, input.OccurredOn, "registrar receita");

        var account = await RequireFinancialAccountAsync(input.UserId, input.FinancialAccountId, cancellationToken);
        var category = await RequireCategoryAsync(input.UserId, input.TransactionCategoryId, TransactionCategoryType.Income, cancellationToken);
        var nowUtc = _dateTimeProvider.UtcNow;

        account.ApplyDelta(input.Amount, nowUtc);

        var transaction = Transaction.CreateIncome(
            input.UserId,
            account.Id,
            category.Id,
            input.Amount,
            input.OccurredOn,
            input.Description,
            nowUtc);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        return Map(transaction);
    }

    public async Task<TransactionDto> RegisterExpenseAsync(CreateExpenseTransactionInput input, CancellationToken cancellationToken)
    {
        ValidateCommonInput(input.UserId, input.Amount, input.OccurredOn, "registrar despesa");

        var account = await RequireFinancialAccountAsync(input.UserId, input.FinancialAccountId, cancellationToken);
        var category = await RequireCategoryAsync(input.UserId, input.TransactionCategoryId, TransactionCategoryType.Expense, cancellationToken);
        var nowUtc = _dateTimeProvider.UtcNow;

        account.ApplyDelta(-input.Amount, nowUtc);

        var transaction = Transaction.CreateExpense(
            input.UserId,
            account.Id,
            category.Id,
            input.Amount,
            input.OccurredOn,
            input.Description,
            nowUtc);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        return Map(transaction);
    }

    public async Task<TransactionDto> RegisterTransferAsync(CreateTransferTransactionInput input, CancellationToken cancellationToken)
    {
        ValidateCommonInput(input.UserId, input.Amount, input.OccurredOn, "registrar transferencia");

        if (input.SourceFinancialAccountId == Guid.Empty)
        {
            throw new AppValidationException("A conta de origem da transferencia e obrigatoria.");
        }

        if (input.DestinationFinancialAccountId == Guid.Empty)
        {
            throw new AppValidationException("A conta de destino da transferencia e obrigatoria.");
        }

        if (input.SourceFinancialAccountId == input.DestinationFinancialAccountId)
        {
            throw new AppValidationException("A transferencia exige contas de origem e destino diferentes.");
        }

        var sourceAccount = await RequireFinancialAccountAsync(input.UserId, input.SourceFinancialAccountId, cancellationToken);
        var destinationAccount = await RequireFinancialAccountAsync(input.UserId, input.DestinationFinancialAccountId, cancellationToken);
        var nowUtc = _dateTimeProvider.UtcNow;

        sourceAccount.ApplyDelta(-input.Amount, nowUtc);
        destinationAccount.ApplyDelta(input.Amount, nowUtc);

        var transaction = Transaction.CreateTransfer(
            input.UserId,
            sourceAccount.Id,
            destinationAccount.Id,
            input.Amount,
            input.OccurredOn,
            input.Description,
            nowUtc);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        return Map(transaction);
    }

    public async Task<IReadOnlyList<TransactionDto>> GetByPeriodAsync(GetTransactionsByPeriodInput input, CancellationToken cancellationToken)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para listar transacoes.");
        }

        if (input.From == default || input.To == default)
        {
            throw new AppValidationException("O periodo informado para listagem de transacoes e obrigatorio.");
        }

        if (input.From > input.To)
        {
            throw new AppValidationException("A data inicial nao pode ser maior que a data final.");
        }

        var transactions = await _transactionRepository.GetByUserAndPeriodAsync(
            input.UserId,
            input.From,
            input.To,
            input.Type,
            input.FinancialAccountId,
            cancellationToken);

        return transactions.Select(Map).ToList();
    }

    private async Task<FinancialAccount> RequireFinancialAccountAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken)
    {
        var account = await _financialAccountRepository.GetByUserIdAndIdAsync(userId, financialAccountId, cancellationToken);

        if (account is null)
        {
            throw new AppValidationException("A conta financeira informada nao foi encontrada para o usuario autenticado.");
        }

        if (!account.IsActive)
        {
            throw new AppValidationException("A conta financeira informada esta inativa.");
        }

        return account;
    }

    private async Task<TransactionCategory> RequireCategoryAsync(
        Guid userId,
        Guid transactionCategoryId,
        TransactionCategoryType expectedType,
        CancellationToken cancellationToken)
    {
        if (transactionCategoryId == Guid.Empty)
        {
            throw new AppValidationException("A categoria da transacao e obrigatoria.");
        }

        var category = await _transactionCategoryRepository.GetByUserIdAndIdAsync(userId, transactionCategoryId, cancellationToken);

        if (category is null)
        {
            throw new AppValidationException("A categoria informada nao foi encontrada para o usuario autenticado.");
        }

        if (!category.IsActive)
        {
            throw new AppValidationException("A categoria informada esta inativa.");
        }

        if (category.Type != expectedType)
        {
            throw new AppValidationException("A categoria informada nao e compativel com o tipo da transacao.");
        }

        return category;
    }

    private static TransactionDto Map(Transaction transaction)
    {
        return new TransactionDto(
            transaction.Id,
            transaction.Type,
            transaction.Status,
            transaction.Amount,
            transaction.OccurredOn,
            transaction.Description,
            transaction.FinancialAccountId,
            transaction.TransactionCategoryId,
            transaction.SourceFinancialAccountId,
            transaction.DestinationFinancialAccountId,
            transaction.CreatedAtUtc);
    }

    private static void ValidateCommonInput(Guid userId, decimal amount, DateOnly occurredOn, string action)
    {
        if (userId == Guid.Empty)
        {
            throw new AppUnauthorizedException($"Usuario autenticado nao encontrado para {action}.");
        }

        if (amount <= 0)
        {
            throw new AppValidationException("O valor da transacao deve ser maior que zero.");
        }

        if (occurredOn == default)
        {
            throw new AppValidationException("A data da transacao e obrigatoria.");
        }
    }
}

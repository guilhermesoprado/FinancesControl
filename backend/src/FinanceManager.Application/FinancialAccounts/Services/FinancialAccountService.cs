using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.FinancialAccounts.Contracts;
using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.FinancialAccounts.Services;

public sealed class FinancialAccountService : IFinancialAccountService
{
    private readonly IFinancialAccountRepository _financialAccountRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public FinancialAccountService(
        IFinancialAccountRepository financialAccountRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _financialAccountRepository = financialAccountRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<FinancialAccountDto> CreateAsync(CreateFinancialAccountInput input, CancellationToken cancellationToken)
    {
        ValidateCreateInput(input);

        var financialAccount = FinancialAccount.Create(
            input.UserId,
            input.Name,
            input.Type,
            input.InitialBalance,
            input.InstitutionName,
            input.Description,
            _dateTimeProvider.UtcNow);

        await _financialAccountRepository.AddAsync(financialAccount, cancellationToken);
        await _financialAccountRepository.SaveChangesAsync(cancellationToken);

        return Map(financialAccount);
    }

    public async Task<IReadOnlyList<FinancialAccountDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para listar contas.");
        }

        var accounts = await _financialAccountRepository.GetByUserIdAsync(userId, cancellationToken);
        return accounts.Select(Map).ToList();
    }

    private static FinancialAccountDto Map(FinancialAccount financialAccount)
    {
        return new FinancialAccountDto(
            financialAccount.Id,
            financialAccount.Name,
            financialAccount.Type,
            financialAccount.InitialBalance,
            financialAccount.CurrentBalanceSnapshot,
            financialAccount.IsActive,
            financialAccount.InstitutionName,
            financialAccount.Description,
            financialAccount.CreatedAtUtc);
    }

    private static void ValidateCreateInput(CreateFinancialAccountInput input)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para criar conta.");
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new AppValidationException("O nome da conta e obrigatorio.");
        }

        if (input.InitialBalance < 0)
        {
            throw new AppValidationException("O saldo inicial nao pode ser negativo.");
        }
    }
}

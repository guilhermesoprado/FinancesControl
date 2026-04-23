using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.FinancialAccounts.Contracts;
using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.FinancialAccounts.Services;

public sealed class FinancialAccountService : IFinancialAccountService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IFinancialAccountRepository _financialAccountRepository;
    private readonly IScheduledEntryRepository _scheduledEntryRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public FinancialAccountService(
        IAuditLogRepository auditLogRepository,
        IFinancialAccountRepository financialAccountRepository,
        IScheduledEntryRepository scheduledEntryRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _auditLogRepository = auditLogRepository;
        _financialAccountRepository = financialAccountRepository;
        _scheduledEntryRepository = scheduledEntryRepository;
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
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                input.UserId,
                Domain.Enums.AuditLogEntityType.FinancialAccount,
                financialAccount.Id,
                Domain.Enums.AuditLogAction.Created,
                BuildCreateSummary(financialAccount),
                _dateTimeProvider.UtcNow),
            cancellationToken);
        await _financialAccountRepository.SaveChangesAsync(cancellationToken);

        return Map(financialAccount);
    }

    public async Task<FinancialAccountDto> UpdateAsync(UpdateFinancialAccountInput input, CancellationToken cancellationToken)
    {
        ValidateUpdateInput(input);

        var financialAccount = await _financialAccountRepository.GetByUserIdAndIdAsync(
            input.UserId,
            input.FinancialAccountId,
            cancellationToken);

        if (financialAccount is null)
        {
            throw new AppValidationException("A conta financeira informada nao foi encontrada para o usuario autenticado.");
        }

        var previousName = financialAccount.Name;
        var previousType = financialAccount.Type;
        var previousInstitutionName = financialAccount.InstitutionName;
        var previousDescription = financialAccount.Description;

        try
        {
            financialAccount.Update(
                input.Name,
                input.Type,
                input.InstitutionName,
                input.Description,
                _dateTimeProvider.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new AppValidationException(exception.Message);
        }

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                input.UserId,
                Domain.Enums.AuditLogEntityType.FinancialAccount,
                financialAccount.Id,
                Domain.Enums.AuditLogAction.Updated,
                BuildUpdateSummary(
                    previousName,
                    previousType,
                    previousInstitutionName,
                    previousDescription,
                    financialAccount),
                _dateTimeProvider.UtcNow),
            cancellationToken);
        await _financialAccountRepository.SaveChangesAsync(cancellationToken);
        return Map(financialAccount);
    }

    public async Task<FinancialAccountDto> InactivateAsync(InactivateFinancialAccountInput input, CancellationToken cancellationToken)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para inativar conta.");
        }

        if (input.FinancialAccountId == Guid.Empty)
        {
            throw new AppValidationException("A conta financeira informada e obrigatoria.");
        }

        var financialAccount = await _financialAccountRepository.GetByUserIdAndIdAsync(
            input.UserId,
            input.FinancialAccountId,
            cancellationToken);

        if (financialAccount is null)
        {
            throw new AppValidationException("A conta financeira informada nao foi encontrada para o usuario autenticado.");
        }

        var hasActiveScheduledEntries = await _scheduledEntryRepository.ExistsActiveByUserAndFinancialAccountIdAsync(
            input.UserId,
            financialAccount.Id,
            cancellationToken);

        if (hasActiveScheduledEntries)
        {
            throw new AppValidationException("Nao e possivel inativar uma conta financeira vinculada a lancamentos planejados ativos.");
        }

        var visibleBalance = financialAccount.CurrentBalanceSnapshot ?? financialAccount.InitialBalance;

        try
        {
            financialAccount.Inactivate(_dateTimeProvider.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new AppValidationException(exception.Message);
        }

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                input.UserId,
                Domain.Enums.AuditLogEntityType.FinancialAccount,
                financialAccount.Id,
                Domain.Enums.AuditLogAction.Inactivated,
                BuildInactivationSummary(financialAccount, visibleBalance),
                _dateTimeProvider.UtcNow),
            cancellationToken);
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

    private static void ValidateUpdateInput(UpdateFinancialAccountInput input)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para editar conta.");
        }

        if (input.FinancialAccountId == Guid.Empty)
        {
            throw new AppValidationException("A conta financeira informada e obrigatoria.");
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new AppValidationException("O nome da conta e obrigatorio.");
        }
    }

    private static string BuildCreateSummary(FinancialAccount financialAccount)
    {
        return $"Conta criada: nome='{financialAccount.Name}', tipo='{financialAccount.Type}', instituicao='{financialAccount.InstitutionName ?? "-"}'.";
    }

    private static string BuildUpdateSummary(
        string previousName,
        Domain.Enums.FinancialAccountType previousType,
        string? previousInstitutionName,
        string? previousDescription,
        FinancialAccount financialAccount)
    {
        return $"Conta atualizada: nome='{previousName}' -> '{financialAccount.Name}', tipo='{previousType}' -> '{financialAccount.Type}', instituicao='{previousInstitutionName ?? "-"}' -> '{financialAccount.InstitutionName ?? "-"}', descricao='{previousDescription ?? "-"}' -> '{financialAccount.Description ?? "-"}'.";
    }

    private static string BuildInactivationSummary(FinancialAccount financialAccount, decimal visibleBalance)
    {
        return $"Conta inativada: nome='{financialAccount.Name}', tipo='{financialAccount.Type}', saldo_visivel='{visibleBalance:0.00}'.";
    }
}

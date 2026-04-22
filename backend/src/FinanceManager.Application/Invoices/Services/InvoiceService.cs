using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.Invoices.Contracts;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Invoices.Services;

public sealed class InvoiceService : IInvoiceService
{
    private const int MaxInstallmentCount = 12;
    private const decimal DefaultPenaltyRate = 0.02m;
    private const decimal DefaultLateInterestMonthlyRate = 0.01m;
    private const decimal DefaultRevolvingInterestMonthlyRate = 0.12m;

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IFinancialAccountRepository _financialAccountRepository;
    private readonly ITransactionCategoryRepository _transactionCategoryRepository;
    private readonly ICreditCardExpenseRepository _creditCardExpenseRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        ICreditCardRepository creditCardRepository,
        IFinancialAccountRepository financialAccountRepository,
        ITransactionCategoryRepository transactionCategoryRepository,
        ICreditCardExpenseRepository creditCardExpenseRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _invoiceRepository = invoiceRepository;
        _creditCardRepository = creditCardRepository;
        _financialAccountRepository = financialAccountRepository;
        _transactionCategoryRepository = transactionCategoryRepository;
        _creditCardExpenseRepository = creditCardExpenseRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceInput input, CancellationToken cancellationToken)
    {
        ValidateCreateInput(input);

        var creditCard = await _creditCardRepository.GetByUserIdAndIdAsync(
            input.UserId,
            input.CreditCardId,
            cancellationToken);

        if (creditCard is null)
        {
            throw new AppValidationException("O cartao informado nao foi encontrado.");
        }

        var exists = await _invoiceRepository.ExistsByUserCreditCardAndReferenceAsync(
            input.UserId,
            input.CreditCardId,
            input.ReferenceYear,
            input.ReferenceMonth,
            cancellationToken);

        if (exists)
        {
            throw new AppValidationException("Ja existe uma fatura para este cartao no mes de referencia informado.");
        }

        var invoice = OpenInvoice(input.UserId, creditCard, input.ReferenceYear, input.ReferenceMonth);

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        await _invoiceRepository.SaveChangesAsync(cancellationToken);

        return Map(invoice, creditCard.Name, creditCard.Brand);
    }

    public async Task<InvoiceDto> CloseAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para fechar fatura.");
        }

        if (invoiceId == Guid.Empty)
        {
            throw new AppValidationException("A fatura informada e obrigatoria.");
        }

        var invoice = await _invoiceRepository.GetByUserAndIdAsync(userId, invoiceId, cancellationToken);
        if (invoice is null)
        {
            throw new AppValidationException("A fatura informada nao foi encontrada.");
        }

        var nowUtc = _dateTimeProvider.UtcNow;

        try
        {
            invoice.Close(nowUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new AppValidationException(exception.Message);
        }

        await _invoiceRepository.SaveChangesAsync(cancellationToken);

        var creditCard = await _creditCardRepository.GetByUserIdAndIdAsync(userId, invoice.CreditCardId, cancellationToken);
        return Map(invoice, creditCard?.Name ?? "Cartao nao encontrado", creditCard?.Brand);
    }

    public async Task<IReadOnlyList<InvoiceDto>> GetByUserAsync(Guid userId, Guid? creditCardId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para listar faturas.");
        }

        var invoices = await _invoiceRepository.GetByUserAsync(userId, creditCardId, cancellationToken);
        var creditCards = await _creditCardRepository.GetByUserIdAsync(userId, cancellationToken);
        var creditCardMap = creditCards.ToDictionary(x => x.Id);

        return invoices
            .Select(invoice =>
            {
                creditCardMap.TryGetValue(invoice.CreditCardId, out var creditCard);
                return Map(invoice, creditCard?.Name ?? "Cartao nao encontrado", creditCard?.Brand);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<CreditCardExpenseDto>> GetCardExpensesByUserAsync(Guid userId, Guid? creditCardId, Guid? invoiceId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para listar compras do cartao.");
        }

        var expenses = await _creditCardExpenseRepository.GetByUserAsync(userId, cancellationToken);
        var categories = await _transactionCategoryRepository.GetByUserIdAsync(userId, cancellationToken);
        var categoryMap = categories.ToDictionary(x => x.Id, x => x.Name);

        IEnumerable<CreditCardExpense> query = expenses;

        if (creditCardId.HasValue)
        {
            query = query.Where(expense => expense.CreditCardId == creditCardId.Value);
        }

        if (invoiceId.HasValue)
        {
            query = query.Where(expense => expense.InvoiceId == invoiceId.Value);
        }

        return query
            .OrderByDescending(expense => expense.OccurredOn)
            .ThenBy(expense => expense.InstallmentNumber)
            .ThenByDescending(expense => expense.CreatedAtUtc)
            .Select(expense => new CreditCardExpenseDto(
                expense.Id,
                expense.CreditCardId,
                expense.InvoiceId,
                expense.TransactionCategoryId,
                categoryMap.TryGetValue(expense.TransactionCategoryId, out var categoryName) ? categoryName : "Categoria nao encontrada",
                expense.InstallmentGroupId,
                expense.InstallmentNumber,
                expense.InstallmentCount,
                expense.Amount,
                expense.OccurredOn,
                expense.Description,
                expense.CreatedAtUtc))
            .ToList();
    }

    public async Task<InvoiceDto> PayAsync(PayInvoiceInput input, CancellationToken cancellationToken)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para pagar fatura.");
        }

        if (input.InvoiceId == Guid.Empty)
        {
            throw new AppValidationException("A fatura informada e obrigatoria.");
        }

        if (input.FinancialAccountId == Guid.Empty)
        {
            throw new AppValidationException("A conta financeira de pagamento e obrigatoria.");
        }

        if (input.Amount <= 0m)
        {
            throw new AppValidationException("O valor informado para pagamento deve ser maior que zero.");
        }

        var invoice = await _invoiceRepository.GetByUserAndIdAsync(input.UserId, input.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            throw new AppValidationException("A fatura informada nao foi encontrada.");
        }

        var financialAccount = await _financialAccountRepository.GetByUserIdAndIdAsync(input.UserId, input.FinancialAccountId, cancellationToken);
        if (financialAccount is null)
        {
            throw new AppValidationException("A conta financeira informada nao foi encontrada.");
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        invoice.ApplyFinancialCharges(DateOnly.FromDateTime(nowUtc), nowUtc, DefaultPenaltyRate, DefaultLateInterestMonthlyRate, DefaultRevolvingInterestMonthlyRate);
        invoice.ApplyPayment(financialAccount.Id, input.Amount, nowUtc);
        financialAccount.ApplyDelta(-input.Amount, nowUtc);

        await _invoiceRepository.SaveChangesAsync(cancellationToken);

        var creditCard = await _creditCardRepository.GetByUserIdAndIdAsync(input.UserId, invoice.CreditCardId, cancellationToken);
        return Map(invoice, creditCard?.Name ?? "Cartao nao encontrado", creditCard?.Brand);
    }

    public async Task<InvoiceDto> AdjustAsync(AdjustInvoiceInput input, CancellationToken cancellationToken)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para ajustar fatura.");
        }

        if (input.InvoiceId == Guid.Empty)
        {
            throw new AppValidationException("A fatura informada e obrigatoria.");
        }

        if (input.Amount <= 0m)
        {
            throw new AppValidationException("O valor do ajuste deve ser maior que zero.");
        }

        var invoice = await _invoiceRepository.GetByUserAndIdAsync(input.UserId, input.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            throw new AppValidationException("A fatura informada nao foi encontrada.");
        }

        var nowUtc = _dateTimeProvider.UtcNow;

        try
        {
            invoice.ApplyAdjustment(input.AdjustmentType, input.Amount, nowUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new AppValidationException(exception.Message);
        }

        await _invoiceRepository.SaveChangesAsync(cancellationToken);

        var creditCard = await _creditCardRepository.GetByUserIdAndIdAsync(input.UserId, invoice.CreditCardId, cancellationToken);
        return Map(invoice, creditCard?.Name ?? "Cartao nao encontrado", creditCard?.Brand);
    }
    public async Task<InvoiceDto> RegisterCardExpenseAsync(RegisterCardExpenseInput input, CancellationToken cancellationToken)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para registrar compra no cartao.");
        }

        if (input.CreditCardId == Guid.Empty)
        {
            throw new AppValidationException("O cartao da compra e obrigatorio.");
        }

        if (input.TransactionCategoryId == Guid.Empty)
        {
            throw new AppValidationException("A categoria da compra no cartao e obrigatoria.");
        }

        if (input.Amount <= 0)
        {
            throw new AppValidationException("O valor da compra no cartao deve ser maior que zero.");
        }

        if (input.OccurredOn == default)
        {
            throw new AppValidationException("A data da compra no cartao e obrigatoria.");
        }

        if (input.InstallmentCount <= 0 || input.InstallmentCount > MaxInstallmentCount)
        {
            throw new AppValidationException("A quantidade de parcelas deve estar entre 1 e 12.");
        }

        var creditCard = await RequireActiveCreditCardAsync(input.UserId, input.CreditCardId, cancellationToken);
        var category = await RequireExpenseCategoryAsync(input.UserId, input.TransactionCategoryId, cancellationToken);
        var nowUtc = _dateTimeProvider.UtcNow;
        var installmentGroupId = Guid.NewGuid();
        var installmentAmounts = SplitInstallments(input.Amount, input.InstallmentCount);
        Invoice? lastInvoice = null;

        for (var installmentIndex = 0; installmentIndex < input.InstallmentCount; installmentIndex += 1)
        {
            var (referenceYear, referenceMonth) = ResolveInstallmentReference(input.OccurredOn, creditCard.ClosingDay, installmentIndex);
            var invoice = await RequireOrOpenInvoiceByReferenceAsync(
                input.UserId,
                creditCard,
                referenceYear,
                referenceMonth,
                installmentIndex == 0 ? input.TargetInvoiceId : null,
                cancellationToken);

            var allowClosedInvoice = input.TargetInvoiceId.HasValue
                && installmentIndex == 0
                && invoice.Id == input.TargetInvoiceId.Value
                && invoice.Status != InvoiceStatus.Open;

            invoice.AddCharge(installmentAmounts[installmentIndex], nowUtc, allowClosedInvoice);

            var expense = CreditCardExpense.Register(
                input.UserId,
                creditCard.Id,
                invoice.Id,
                category.Id,
                installmentGroupId,
                installmentIndex + 1,
                input.InstallmentCount,
                installmentAmounts[installmentIndex],
                input.OccurredOn,
                BuildInstallmentDescription(input.Description, installmentIndex + 1, input.InstallmentCount),
                nowUtc);

            await _creditCardExpenseRepository.AddAsync(expense, cancellationToken);
            lastInvoice = invoice;
        }

        await _invoiceRepository.SaveChangesAsync(cancellationToken);

        return Map(lastInvoice!, creditCard.Name, creditCard.Brand);
    }

    private async Task<CreditCard> RequireActiveCreditCardAsync(Guid userId, Guid creditCardId, CancellationToken cancellationToken)
    {
        var creditCard = await _creditCardRepository.GetByUserIdAndIdAsync(userId, creditCardId, cancellationToken);

        if (creditCard is null)
        {
            throw new AppValidationException("O cartao informado nao foi encontrado.");
        }

        if (!creditCard.IsActive)
        {
            throw new AppValidationException("O cartao informado esta inativo.");
        }

        return creditCard;
    }

    private async Task<TransactionCategory> RequireExpenseCategoryAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken)
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

        if (category.Type != TransactionCategoryType.Expense)
        {
            throw new AppValidationException("A categoria informada nao e compativel com compra de cartao.");
        }

        return category;
    }

    private async Task<Invoice> RequireOrOpenInvoiceByReferenceAsync(
        Guid userId,
        CreditCard creditCard,
        int referenceYear,
        int referenceMonth,
        Guid? targetInvoiceId,
        CancellationToken cancellationToken)
    {
        var existing = await _invoiceRepository.GetByUserCreditCardAndReferenceAsync(
            userId,
            creditCard.Id,
            referenceYear,
            referenceMonth,
            cancellationToken);

        if (existing is null)
        {
            var invoice = OpenInvoice(userId, creditCard, referenceYear, referenceMonth);
            await _invoiceRepository.AddAsync(invoice, cancellationToken);
            return invoice;
        }

        if (existing.Status == InvoiceStatus.Open)
        {
            return existing;
        }

        if (targetInvoiceId.HasValue)
        {
            return await RequireTargetInvoiceAsync(userId, creditCard, existing, targetInvoiceId.Value, cancellationToken);
        }

        return await RequireOrOpenNextInvoiceAsync(userId, creditCard, referenceYear, referenceMonth, cancellationToken);
    }

    private async Task<Invoice> RequireTargetInvoiceAsync(
        Guid userId,
        CreditCard creditCard,
        Invoice referenceInvoice,
        Guid targetInvoiceId,
        CancellationToken cancellationToken)
    {
        var targetInvoice = await _invoiceRepository.GetByUserAndIdAsync(userId, targetInvoiceId, cancellationToken);
        if (targetInvoice is null)
        {
            throw new AppValidationException("A fatura de destino informada nao foi encontrada.");
        }

        if (targetInvoice.CreditCardId != creditCard.Id)
        {
            throw new AppValidationException("A fatura de destino precisa pertencer ao mesmo cartao da compra.");
        }

        if (targetInvoice.Id == referenceInvoice.Id)
        {
            if (targetInvoice.Status == InvoiceStatus.Paid)
            {
                throw new AppValidationException("Nao e possivel incluir compras em uma fatura ja paga.");
            }

            return targetInvoice;
        }

        if (targetInvoice.Status != InvoiceStatus.Open)
        {
            throw new AppValidationException("A fatura de destino alternativa precisa estar aberta.");
        }

        if (targetInvoice.ReferenceYear < referenceInvoice.ReferenceYear
            || (targetInvoice.ReferenceYear == referenceInvoice.ReferenceYear && targetInvoice.ReferenceMonth < referenceInvoice.ReferenceMonth))
        {
            throw new AppValidationException("A fatura de destino alternativa precisa ser igual ou posterior ao ciclo original da compra.");
        }

        return targetInvoice;
    }

    private async Task<Invoice> RequireOrOpenNextInvoiceAsync(
        Guid userId,
        CreditCard creditCard,
        int referenceYear,
        int referenceMonth,
        CancellationToken cancellationToken)
    {
        var currentReference = new DateOnly(referenceYear, referenceMonth, 1);

        for (var offset = 1; offset <= 24; offset += 1)
        {
            var candidateReference = currentReference.AddMonths(offset);
            var existing = await _invoiceRepository.GetByUserCreditCardAndReferenceAsync(
                userId,
                creditCard.Id,
                candidateReference.Year,
                candidateReference.Month,
                cancellationToken);

            if (existing is null)
            {
                var invoice = OpenInvoice(userId, creditCard, candidateReference.Year, candidateReference.Month);
                await _invoiceRepository.AddAsync(invoice, cancellationToken);
                return invoice;
            }

            if (existing.Status == InvoiceStatus.Open)
            {
                return existing;
            }
        }

        throw new AppValidationException("Nao foi possivel encontrar uma proxima fatura aberta para receber o lancamento do cartao.");
    }

    private Invoice OpenInvoice(Guid userId, CreditCard creditCard, int referenceYear, int referenceMonth)
    {
        var closingDate = BuildClosingDate(referenceYear, referenceMonth, creditCard.ClosingDay);
        var previousClosingDate = BuildClosingDate(closingDate.AddMonths(-1).Year, closingDate.AddMonths(-1).Month, creditCard.ClosingDay);
        var periodStart = previousClosingDate.AddDays(1);
        var dueDate = BuildDueDate(referenceYear, referenceMonth, creditCard.ClosingDay, creditCard.DueDay);

        return Invoice.Open(
            userId,
            creditCard.Id,
            referenceYear,
            referenceMonth,
            periodStart,
            closingDate,
            closingDate,
            dueDate,
            _dateTimeProvider.UtcNow);
    }

    private static (int ReferenceYear, int ReferenceMonth) ResolveReference(DateOnly occurredOn, int closingDay)
    {
        var closingDate = BuildClosingDate(occurredOn.Year, occurredOn.Month, closingDay);
        if (occurredOn <= closingDate)
        {
            return (occurredOn.Year, occurredOn.Month);
        }

        var nextReference = new DateOnly(occurredOn.Year, occurredOn.Month, 1).AddMonths(1);
        return (nextReference.Year, nextReference.Month);
    }

    private static (int ReferenceYear, int ReferenceMonth) ResolveInstallmentReference(DateOnly occurredOn, int closingDay, int installmentOffset)
    {
        var (referenceYear, referenceMonth) = ResolveReference(occurredOn, closingDay);
        var referenceDate = new DateOnly(referenceYear, referenceMonth, 1).AddMonths(installmentOffset);
        return (referenceDate.Year, referenceDate.Month);
    }

    private static IReadOnlyList<decimal> SplitInstallments(decimal totalAmount, int installmentCount)
    {
        if (installmentCount == 1)
        {
            return [decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)];
        }

        var baseAmount = decimal.Round(totalAmount / installmentCount, 2, MidpointRounding.ToZero);
        var amounts = new List<decimal>(installmentCount);
        var accumulated = 0m;

        for (var installmentIndex = 1; installmentIndex <= installmentCount; installmentIndex += 1)
        {
            if (installmentIndex == installmentCount)
            {
                amounts.Add(decimal.Round(totalAmount - accumulated, 2, MidpointRounding.AwayFromZero));
                continue;
            }

            amounts.Add(baseAmount);
            accumulated += baseAmount;
        }

        return amounts;
    }

    private static string? BuildInstallmentDescription(string? description, int installmentNumber, int installmentCount)
    {
        var installmentLabel = $"Parcela {installmentNumber}/{installmentCount}";
        if (string.IsNullOrWhiteSpace(description))
        {
            return installmentCount > 1 ? installmentLabel : null;
        }

        return installmentCount > 1
            ? $"{description.Trim()} · {installmentLabel}"
            : description.Trim();
    }

    private static InvoiceDto Map(Invoice invoice, string creditCardName, string? creditCardBrand)
    {
        return new InvoiceDto(
            invoice.Id,
            invoice.CreditCardId,
            creditCardName,
            creditCardBrand,
            invoice.ReferenceYear,
            invoice.ReferenceMonth,
            invoice.PeriodStart,
            invoice.PeriodEnd,
            invoice.ClosingDate,
            invoice.DueDate,
            invoice.TotalAmount,
            invoice.PaidAmount,
            invoice.RemainingAmount,
            invoice.SuggestedMinimumPaymentAmount,
            invoice.LateFeeAppliedAmount,
            invoice.LateInterestAppliedAmount,
            invoice.RevolvingInterestAppliedAmount,
            invoice.Status,
            invoice.PaidFromFinancialAccountId,
            invoice.PaidAtUtc,
            invoice.ClosedAtUtc,
            invoice.CreatedAtUtc);
    }

    private static void ValidateCreateInput(CreateInvoiceInput input)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para criar fatura.");
        }

        if (input.CreditCardId == Guid.Empty)
        {
            throw new AppValidationException("O cartao da fatura e obrigatorio.");
        }

        if (input.ReferenceYear < 2000 || input.ReferenceYear > 9999)
        {
            throw new AppValidationException("O ano de referencia da fatura e invalido.");
        }

        if (input.ReferenceMonth < 1 || input.ReferenceMonth > 12)
        {
            throw new AppValidationException("O mes de referencia da fatura e invalido.");
        }
    }

    private static DateOnly BuildClosingDate(int year, int month, int closingDay)
    {
        var day = Math.Min(closingDay, DateTime.DaysInMonth(year, month));
        return new DateOnly(year, month, day);
    }

    private static DateOnly BuildDueDate(int referenceYear, int referenceMonth, int closingDay, int dueDay)
    {
        var dueMonthDate = dueDay > closingDay
            ? new DateOnly(referenceYear, referenceMonth, 1)
            : new DateOnly(referenceYear, referenceMonth, 1).AddMonths(1);

        var dueDayClamped = Math.Min(dueDay, DateTime.DaysInMonth(dueMonthDate.Year, dueMonthDate.Month));
        return new DateOnly(dueMonthDate.Year, dueMonthDate.Month, dueDayClamped);
    }
}







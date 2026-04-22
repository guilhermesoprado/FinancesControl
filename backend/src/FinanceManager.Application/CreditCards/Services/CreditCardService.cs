using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.CreditCards.Contracts;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.CreditCards.Services;

public sealed class CreditCardService : ICreditCardService
{
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICreditCardExpenseRepository _creditCardExpenseRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreditCardService(
        ICreditCardRepository creditCardRepository,
        IInvoiceRepository invoiceRepository,
        ICreditCardExpenseRepository creditCardExpenseRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _creditCardRepository = creditCardRepository;
        _invoiceRepository = invoiceRepository;
        _creditCardExpenseRepository = creditCardExpenseRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<CreditCardDto> CreateAsync(CreateCreditCardInput input, CancellationToken cancellationToken)
    {
        ValidateCreateInput(input);

        var creditCard = CreditCard.Create(
            input.UserId,
            input.Name,
            input.Brand,
            input.CreditLimit,
            input.ClosingDay,
            input.DueDay,
            input.Description,
            _dateTimeProvider.UtcNow);

        await _creditCardRepository.AddAsync(creditCard, cancellationToken);
        await _creditCardRepository.SaveChangesAsync(cancellationToken);

        return Map(creditCard);
    }

    public async Task<IReadOnlyList<CreditCardDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para listar cartoes.");
        }

        var creditCards = await _creditCardRepository.GetByUserIdAsync(userId, cancellationToken);
        return creditCards.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<CreditCardOverviewDto>> GetOverviewByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para consultar a situacao dos cartoes.");
        }

        var creditCards = await _creditCardRepository.GetByUserIdAsync(userId, cancellationToken);
        var invoices = await _invoiceRepository.GetByUserAsync(userId, null, cancellationToken);
        var expenses = await _creditCardExpenseRepository.GetByUserAsync(userId, cancellationToken);

        return creditCards.Select(card =>
        {
            var cardInvoices = invoices.Where(invoice => invoice.CreditCardId == card.Id).ToList();
            var cardExpenses = expenses.Where(expense => expense.CreditCardId == card.Id).ToList();
            var latestInvoice = cardInvoices
                .OrderByDescending(invoice => invoice.ReferenceYear)
                .ThenByDescending(invoice => invoice.ReferenceMonth)
                .ThenByDescending(invoice => invoice.CreatedAtUtc)
                .FirstOrDefault();
            var openInvoiceAmount = cardInvoices
                .Where(invoice => invoice.Status == InvoiceStatus.Open)
                .Sum(invoice => invoice.TotalAmount);
            var lastPurchase = cardExpenses
                .OrderByDescending(expense => expense.OccurredOn)
                .FirstOrDefault();

            return new CreditCardOverviewDto(
                card.Id,
                card.Name,
                card.Brand,
                card.CreditLimit,
                card.ClosingDay,
                card.DueDay,
                card.IsActive,
                card.Description,
                openInvoiceAmount,
                cardInvoices.Count(invoice => invoice.Status == InvoiceStatus.Open),
                cardInvoices.Count,
                cardExpenses.Sum(expense => expense.Amount),
                cardExpenses.Count,
                latestInvoice?.ReferenceYear,
                latestInvoice?.ReferenceMonth,
                lastPurchase?.OccurredOn,
                card.CreatedAtUtc);
        }).ToList();
    }

    private static CreditCardDto Map(CreditCard creditCard)
    {
        return new CreditCardDto(
            creditCard.Id,
            creditCard.Name,
            creditCard.Brand,
            creditCard.CreditLimit,
            creditCard.ClosingDay,
            creditCard.DueDay,
            creditCard.IsActive,
            creditCard.Description,
            creditCard.CreatedAtUtc);
    }

    private static void ValidateCreateInput(CreateCreditCardInput input)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado para criar cartao.");
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new AppValidationException("O nome do cartao e obrigatorio.");
        }

        if (input.CreditLimit < 0)
        {
            throw new AppValidationException("O limite do cartao nao pode ser negativo.");
        }

        if (input.ClosingDay < 1 || input.ClosingDay > 31)
        {
            throw new AppValidationException("O dia de fechamento deve estar entre 1 e 31.");
        }

        if (input.DueDay < 1 || input.DueDay > 31)
        {
            throw new AppValidationException("O dia de vencimento deve estar entre 1 e 31.");
        }
    }
}

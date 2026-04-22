using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.Invoices.Contracts;
using FinanceManager.Application.Invoices.Services;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Tests;

public sealed class InvoicePaymentServiceTests
{
    [Fact]
    public async Task PayAsync_ShouldMarkInvoiceAsPaidAndReduceAccountBalanceWhenAmountExists()
    {
        var nowUtc = new DateTime(2026, 4, 9, 19, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var account = FinancialAccount.Create(userId, "Conta", FinancialAccountType.BankAccount, 500m, null, null, nowUtc.AddDays(-10));
        var creditCard = CreditCard.Create(userId, "Cartao", null, 2500m, 10, 18, null, nowUtc.AddDays(-5));
        var invoice = CreateInvoiceWithAmount(userId, creditCard.Id, 120m, nowUtc.AddDays(-1));
        var invoiceRepository = new FakeInvoiceRepository(invoice);
        var service = new InvoiceService(
            invoiceRepository,
            new FakeCreditCardRepository(creditCard),
            new FakeFinancialAccountRepository(account),
            new FakeTransactionCategoryRepository(),
            new FakeCreditCardExpenseRepository(),
            new FakeDateTimeProvider(nowUtc));

        var result = await service.PayAsync(new PayInvoiceInput(userId, invoice.Id, account.Id, 120m), CancellationToken.None);

        Assert.Equal(InvoiceStatus.Paid, result.Status);
        Assert.Equal(account.Id, result.PaidFromFinancialAccountId);
        Assert.Equal(120m, result.PaidAmount);
        Assert.Equal(0m, result.RemainingAmount);
        Assert.Equal(380m, account.CurrentBalanceSnapshot);
        Assert.Equal(1, invoiceRepository.SaveChangesCalls);
    }

    [Fact]
    public async Task PayAsync_ShouldAllowPartialPaymentAndKeepRemainingAmount()
    {
        var nowUtc = new DateTime(2026, 4, 9, 19, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var account = FinancialAccount.Create(userId, "Conta", FinancialAccountType.BankAccount, 500m, null, null, nowUtc.AddDays(-10));
        var creditCard = CreditCard.Create(userId, "Cartao", null, 2500m, 10, 18, null, nowUtc.AddDays(-5));
        var invoice = CreateInvoiceWithAmount(userId, creditCard.Id, 120m, nowUtc.AddDays(-1));
        var service = new InvoiceService(
            new FakeInvoiceRepository(invoice),
            new FakeCreditCardRepository(creditCard),
            new FakeFinancialAccountRepository(account),
            new FakeTransactionCategoryRepository(),
            new FakeCreditCardExpenseRepository(),
            new FakeDateTimeProvider(nowUtc));

        var result = await service.PayAsync(new PayInvoiceInput(userId, invoice.Id, account.Id, 30m), CancellationToken.None);

        Assert.Equal(InvoiceStatus.PartiallyPaid, result.Status);
        Assert.Equal(30m, result.PaidAmount);
        Assert.Equal(90m, result.RemainingAmount);
        Assert.Equal(470m, account.CurrentBalanceSnapshot);
    }

    [Fact]
    public async Task PayAsync_ShouldRejectMissingAccount()
    {
        var nowUtc = new DateTime(2026, 4, 9, 19, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var creditCard = CreditCard.Create(userId, "Cartao", null, 2500m, 10, 18, null, nowUtc.AddDays(-5));
        var invoice = CreateInvoiceWithAmount(userId, creditCard.Id, 80m, nowUtc.AddDays(-1));
        var service = new InvoiceService(
            new FakeInvoiceRepository(invoice),
            new FakeCreditCardRepository(creditCard),
            new FakeFinancialAccountRepository(),
            new FakeTransactionCategoryRepository(),
            new FakeCreditCardExpenseRepository(),
            new FakeDateTimeProvider(nowUtc));

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => service.PayAsync(new PayInvoiceInput(userId, invoice.Id, Guid.NewGuid(), 80m), CancellationToken.None));
        Assert.Equal("A conta financeira informada nao foi encontrada.", exception.Message);
    }

    private static Invoice CreateInvoiceWithAmount(Guid userId, Guid creditCardId, decimal totalAmount, DateTime nowUtc)
    {
        var invoice = Invoice.Open(userId, creditCardId, 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), nowUtc);
        invoice.AddCharge(totalAmount, nowUtc);
        return invoice;
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        private readonly Dictionary<Guid, Invoice> _invoices;

        public FakeInvoiceRepository(params Invoice[] invoices)
        {
            _invoices = invoices.ToDictionary(x => x.Id);
        }

        public int SaveChangesCalls { get; private set; }

        public Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
        {
            _invoices[invoice.Id] = invoice;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByUserCreditCardAndReferenceAsync(Guid userId, Guid creditCardId, int referenceYear, int referenceMonth, CancellationToken cancellationToken)
        {
            var exists = _invoices.Values.Any(x => x.UserId == userId && x.CreditCardId == creditCardId && x.ReferenceYear == referenceYear && x.ReferenceMonth == referenceMonth);
            return Task.FromResult(exists);
        }

        public Task<Invoice?> GetByUserAndIdAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken)
        {
            _invoices.TryGetValue(invoiceId, out var invoice);
            return Task.FromResult(invoice is not null && invoice.UserId == userId ? invoice : null);
        }

        public Task<Invoice?> GetByUserCreditCardAndReferenceAsync(Guid userId, Guid creditCardId, int referenceYear, int referenceMonth, CancellationToken cancellationToken)
        {
            var invoice = _invoices.Values.FirstOrDefault(x => x.UserId == userId && x.CreditCardId == creditCardId && x.ReferenceYear == referenceYear && x.ReferenceMonth == referenceMonth);
            return Task.FromResult(invoice);
        }

        public Task<IReadOnlyList<Invoice>> GetByUserAsync(Guid userId, Guid? creditCardId, CancellationToken cancellationToken)
        {
            IEnumerable<Invoice> query = _invoices.Values.Where(x => x.UserId == userId);
            if (creditCardId.HasValue) {
                query = query.Where(x => x.CreditCardId == creditCardId.Value);
            }
            return Task.FromResult((IReadOnlyList<Invoice>)query.ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCreditCardRepository : ICreditCardRepository
    {
        private readonly Dictionary<Guid, CreditCard> _creditCards;

        public FakeCreditCardRepository(params CreditCard[] creditCards)
        {
            _creditCards = creditCards.ToDictionary(x => x.Id);
        }

        public Task AddAsync(CreditCard creditCard, CancellationToken cancellationToken)
        {
            _creditCards[creditCard.Id] = creditCard;
            return Task.CompletedTask;
        }

        public Task<CreditCard?> GetByUserIdAndIdAsync(Guid userId, Guid creditCardId, CancellationToken cancellationToken)
        {
            _creditCards.TryGetValue(creditCardId, out var creditCard);
            return Task.FromResult(creditCard is not null && creditCard.UserId == userId ? creditCard : null);
        }

        public Task<IReadOnlyList<CreditCard>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult((IReadOnlyList<CreditCard>)_creditCards.Values.Where(x => x.UserId == userId).ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFinancialAccountRepository : IFinancialAccountRepository
    {
        private readonly Dictionary<Guid, FinancialAccount> _accounts;

        public FakeFinancialAccountRepository(params FinancialAccount[] accounts)
        {
            _accounts = accounts.ToDictionary(x => x.Id);
        }

        public Task AddAsync(FinancialAccount financialAccount, CancellationToken cancellationToken)
        {
            _accounts[financialAccount.Id] = financialAccount;
            return Task.CompletedTask;
        }

        public Task<FinancialAccount?> GetByUserIdAndIdAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken)
        {
            _accounts.TryGetValue(financialAccountId, out var account);
            return Task.FromResult(account is not null && account.UserId == userId ? account : null);
        }

        public Task<IReadOnlyList<FinancialAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult((IReadOnlyList<FinancialAccount>)_accounts.Values.Where(x => x.UserId == userId).ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTransactionCategoryRepository : ITransactionCategoryRepository
    {
        public Task AddAsync(TransactionCategory transactionCategory, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> ExistsByUserAndNameAndTypeAsync(Guid userId, string normalizedName, TransactionCategoryType type, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<TransactionCategory?> GetByUserIdAndIdAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken) => Task.FromResult<TransactionCategory?>(null);
        public Task<IReadOnlyList<TransactionCategory>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult((IReadOnlyList<TransactionCategory>)[]);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeCreditCardExpenseRepository : ICreditCardExpenseRepository
    {
        public Task AddAsync(CreditCardExpense expense, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<CreditCardExpense>> GetByUserAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult((IReadOnlyList<CreditCardExpense>)[]);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

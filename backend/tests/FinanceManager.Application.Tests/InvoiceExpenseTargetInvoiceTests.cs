using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Invoices.Services;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Tests;

public sealed class InvoiceExpenseTargetInvoiceTests
{
    [Fact]
    public async Task RegisterCardExpenseAsync_ShouldMoveToNextInvoiceWhenOriginalCycleIsClosedAndNoTargetIsProvided()
    {
        var nowUtc = new DateTime(2026, 4, 10, 18, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var creditCard = CreditCard.Create(userId, "Cartao Azul", "Visa", 3000m, 10, 18, null, nowUtc.AddDays(-10));
        var category = TransactionCategory.CreateUserCategory(userId, "Mercado", TransactionCategoryType.Expense, null, null, nowUtc.AddDays(-12));
        var closedInvoice = Invoice.Open(userId, creditCard.Id, 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), nowUtc.AddDays(-5));
        closedInvoice.AddCharge(100m, nowUtc.AddDays(-4));
        closedInvoice.Close(nowUtc.AddHours(-1));
        var nextOpenInvoice = Invoice.Open(userId, creditCard.Id, 2026, 5, new DateOnly(2026, 4, 11), new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 18), nowUtc.AddDays(-1));
        var invoiceRepository = new FakeInvoiceRepository(closedInvoice, nextOpenInvoice);
        var expenseRepository = new FakeCreditCardExpenseRepository();
        var service = CreateService(invoiceRepository, new FakeCreditCardRepository(creditCard), new FakeTransactionCategoryRepository(category), expenseRepository, nowUtc);

        var result = await service.RegisterCardExpenseAsync(
            new FinanceManager.Application.Invoices.Contracts.RegisterCardExpenseInput(userId, creditCard.Id, category.Id, 50m, new DateOnly(2026, 4, 9), "Farmacia", null, 1),
            CancellationToken.None);

        Assert.Equal(nextOpenInvoice.Id, result.Id);
        Assert.Equal(nextOpenInvoice.Id, expenseRepository.AddedExpenses[^1].InvoiceId);
        Assert.Equal(100m, closedInvoice.TotalAmount);
    }

    [Fact]
    public async Task RegisterCardExpenseAsync_ShouldUseChosenClosedInvoiceWhenTargetInvoiceIsProvided()
    {
        var nowUtc = new DateTime(2026, 4, 10, 18, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var creditCard = CreditCard.Create(userId, "Cartao Azul", "Visa", 3000m, 10, 18, null, nowUtc.AddDays(-10));
        var category = TransactionCategory.CreateUserCategory(userId, "Mercado", TransactionCategoryType.Expense, null, null, nowUtc.AddDays(-12));
        var closedInvoice = Invoice.Open(userId, creditCard.Id, 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), nowUtc.AddDays(-5));
        closedInvoice.AddCharge(100m, nowUtc.AddDays(-4));
        closedInvoice.Close(nowUtc.AddHours(-1));
        var nextOpenInvoice = Invoice.Open(userId, creditCard.Id, 2026, 5, new DateOnly(2026, 4, 11), new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 18), nowUtc.AddDays(-1));
        var expenseRepository = new FakeCreditCardExpenseRepository();
        var service = CreateService(new FakeInvoiceRepository(closedInvoice, nextOpenInvoice), new FakeCreditCardRepository(creditCard), new FakeTransactionCategoryRepository(category), expenseRepository, nowUtc);

        var result = await service.RegisterCardExpenseAsync(
            new FinanceManager.Application.Invoices.Contracts.RegisterCardExpenseInput(userId, creditCard.Id, category.Id, 50m, new DateOnly(2026, 4, 9), "Farmacia", closedInvoice.Id, 1),
            CancellationToken.None);

        Assert.Equal(closedInvoice.Id, result.Id);
        Assert.Equal(closedInvoice.Id, expenseRepository.AddedExpenses[^1].InvoiceId);
        Assert.Equal(150m, result.TotalAmount);
    }

    private static InvoiceService CreateService(
        FakeInvoiceRepository invoiceRepository,
        FakeCreditCardRepository creditCardRepository,
        FakeTransactionCategoryRepository categoryRepository,
        FakeCreditCardExpenseRepository expenseRepository,
        DateTime nowUtc)
    {
        return new InvoiceService(
            invoiceRepository,
            creditCardRepository,
            new FakeFinancialAccountRepository(),
            categoryRepository,
            expenseRepository,
            new FakeDateTimeProvider(nowUtc));
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        private readonly Dictionary<Guid, Invoice> _invoices;
        public FakeInvoiceRepository(params Invoice[] invoices) => _invoices = invoices.ToDictionary(x => x.Id);
        public Task AddAsync(Invoice invoice, CancellationToken cancellationToken) { _invoices[invoice.Id] = invoice; return Task.CompletedTask; }
        public Task<bool> ExistsByUserCreditCardAndReferenceAsync(Guid userId, Guid creditCardId, int referenceYear, int referenceMonth, CancellationToken cancellationToken) => Task.FromResult(_invoices.Values.Any(x => x.UserId == userId && x.CreditCardId == creditCardId && x.ReferenceYear == referenceYear && x.ReferenceMonth == referenceMonth));
        public Task<Invoice?> GetByUserAndIdAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken) { _invoices.TryGetValue(invoiceId, out var invoice); return Task.FromResult(invoice is not null && invoice.UserId == userId ? invoice : null); }
        public Task<Invoice?> GetByUserCreditCardAndReferenceAsync(Guid userId, Guid creditCardId, int referenceYear, int referenceMonth, CancellationToken cancellationToken) => Task.FromResult(_invoices.Values.FirstOrDefault(x => x.UserId == userId && x.CreditCardId == creditCardId && x.ReferenceYear == referenceYear && x.ReferenceMonth == referenceMonth));
        public Task<IReadOnlyList<Invoice>> GetByUserAsync(Guid userId, Guid? creditCardId, CancellationToken cancellationToken) => Task.FromResult((IReadOnlyList<Invoice>)_invoices.Values.Where(x => x.UserId == userId && (!creditCardId.HasValue || x.CreditCardId == creditCardId.Value)).ToList());
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeCreditCardRepository : ICreditCardRepository
    {
        private readonly Dictionary<Guid, CreditCard> _creditCards;
        public FakeCreditCardRepository(params CreditCard[] creditCards) => _creditCards = creditCards.ToDictionary(x => x.Id);
        public Task AddAsync(CreditCard creditCard, CancellationToken cancellationToken) { _creditCards[creditCard.Id] = creditCard; return Task.CompletedTask; }
        public Task<CreditCard?> GetByUserIdAndIdAsync(Guid userId, Guid creditCardId, CancellationToken cancellationToken) { _creditCards.TryGetValue(creditCardId, out var creditCard); return Task.FromResult(creditCard is not null && creditCard.UserId == userId ? creditCard : null); }
        public Task<IReadOnlyList<CreditCard>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult((IReadOnlyList<CreditCard>)_creditCards.Values.Where(x => x.UserId == userId).ToList());
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeFinancialAccountRepository : IFinancialAccountRepository
    {
        public Task AddAsync(FinancialAccount financialAccount, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<FinancialAccount?> GetByUserIdAndIdAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken) => Task.FromResult<FinancialAccount?>(null);
        public Task<IReadOnlyList<FinancialAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult((IReadOnlyList<FinancialAccount>)[]);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTransactionCategoryRepository : ITransactionCategoryRepository
    {
        private readonly Dictionary<Guid, TransactionCategory> _categories;
        public FakeTransactionCategoryRepository(params TransactionCategory[] categories) => _categories = categories.ToDictionary(x => x.Id);
        public Task AddAsync(TransactionCategory transactionCategory, CancellationToken cancellationToken) { _categories[transactionCategory.Id] = transactionCategory; return Task.CompletedTask; }
        public Task<bool> ExistsByUserAndNameAndTypeAsync(Guid userId, string normalizedName, TransactionCategoryType type, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<TransactionCategory?> GetByUserIdAndIdAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken) { _categories.TryGetValue(transactionCategoryId, out var category); return Task.FromResult(category is not null && category.UserId == userId ? category : null); }
        public Task<IReadOnlyList<TransactionCategory>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult((IReadOnlyList<TransactionCategory>)_categories.Values.Where(x => x.UserId == userId).ToList());
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeCreditCardExpenseRepository : ICreditCardExpenseRepository
    {
        public List<CreditCardExpense> AddedExpenses { get; } = [];
        public Task AddAsync(CreditCardExpense expense, CancellationToken cancellationToken) { AddedExpenses.Add(expense); return Task.CompletedTask; }
        public Task<IReadOnlyList<CreditCardExpense>> GetByUserAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult((IReadOnlyList<CreditCardExpense>)AddedExpenses.Where(x => x.UserId == userId).ToList());
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

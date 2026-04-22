using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.Invoices.Contracts;
using FinanceManager.Application.Invoices.Services;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Tests;

public sealed class InvoiceServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistInvoiceWithCalculatedCycle()
    {
        var nowUtc = new DateTime(2026, 4, 9, 15, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var creditCard = CreditCard.Create(userId, "Cartao", "Visa", 2500m, 10, 18, null, nowUtc.AddDays(-1));
        var invoiceRepository = new FakeInvoiceRepository();
        var creditCardRepository = new FakeCreditCardRepository(creditCard);
        var service = CreateService(invoiceRepository, creditCardRepository, nowUtc);

        var result = await service.CreateAsync(
            new CreateInvoiceInput(userId, creditCard.Id, 2026, 4),
            CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 3, 11), result.PeriodStart);
        Assert.Equal(new DateOnly(2026, 4, 10), result.PeriodEnd);
        Assert.Equal(new DateOnly(2026, 4, 18), result.DueDate);
        Assert.Single(invoiceRepository.AddedInvoices);
        Assert.Equal(1, invoiceRepository.SaveChangesCalls);
    }
    [Fact]
    public async Task CreateAsync_ShouldRejectDuplicateInvoice()
    {
        var nowUtc = new DateTime(2026, 4, 9, 15, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var creditCard = CreditCard.Create(userId, "Cartao", null, 2500m, 10, 5, null, nowUtc.AddDays(-1));
        var invoiceRepository = new FakeInvoiceRepository { DuplicateExists = true };
        var service = CreateService(invoiceRepository, new FakeCreditCardRepository(creditCard), nowUtc);

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(
            new CreateInvoiceInput(userId, creditCard.Id, 2026, 4),
            CancellationToken.None));

        Assert.Equal("Ja existe uma fatura para este cartao no mes de referencia informado.", exception.Message);
    }
    [Fact]
    public async Task RegisterCardExpenseAsync_ShouldOpenInvoiceAutomaticallyAndPersistExpense()
    {
        var nowUtc = new DateTime(2026, 4, 9, 15, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var creditCard = CreditCard.Create(userId, "Cartao Azul", "Visa", 3000m, 10, 18, null, nowUtc.AddDays(-1));
        var category = TransactionCategory.CreateUserCategory(userId, "Mercado", TransactionCategoryType.Expense, null, null, nowUtc.AddDays(-2));
        var invoiceRepository = new FakeInvoiceRepository();
        var expenseRepository = new FakeCreditCardExpenseRepository();
        var service = CreateService(
            invoiceRepository,
            new FakeCreditCardRepository(creditCard),
            nowUtc,
            categoryRepository: new FakeTransactionCategoryRepository(category),
            expenseRepository: expenseRepository);

        var result = await service.RegisterCardExpenseAsync(
            new RegisterCardExpenseInput(userId, creditCard.Id, category.Id, 240m, new DateOnly(2026, 4, 11), "Supermercado"),
            CancellationToken.None);

        Assert.Equal(2026, result.ReferenceYear);
        Assert.Equal(5, result.ReferenceMonth);
        Assert.Equal(240m, result.TotalAmount);
        Assert.Single(invoiceRepository.AddedInvoices);
        Assert.Single(expenseRepository.AddedExpenses);
        Assert.Equal(invoiceRepository.AddedInvoices[0].Id, expenseRepository.AddedExpenses[0].InvoiceId);
        Assert.Equal(1, expenseRepository.AddedExpenses[0].InstallmentNumber);
        Assert.Equal(1, expenseRepository.AddedExpenses[0].InstallmentCount);
        Assert.Equal(1, invoiceRepository.SaveChangesCalls);
    }
    [Fact]
    public async Task RegisterCardExpenseAsync_ShouldAccumulateOnExistingInvoice()
    {
        var nowUtc = new DateTime(2026, 4, 9, 15, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var creditCard = CreditCard.Create(userId, "Cartao Azul", "Visa", 3000m, 10, 18, null, nowUtc.AddDays(-1));
        var category = TransactionCategory.CreateUserCategory(userId, "Mercado", TransactionCategoryType.Expense, null, null, nowUtc.AddDays(-2));
        var existingInvoice = Invoice.Open(userId, creditCard.Id, 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), nowUtc.AddDays(-3));
        existingInvoice.AddCharge(100m, nowUtc.AddDays(-2));
        var invoiceRepository = new FakeInvoiceRepository(existingInvoice);
        var service = CreateService(
            invoiceRepository,
            new FakeCreditCardRepository(creditCard),
            nowUtc,
            categoryRepository: new FakeTransactionCategoryRepository(category),
            expenseRepository: new FakeCreditCardExpenseRepository());

        var result = await service.RegisterCardExpenseAsync(
            new RegisterCardExpenseInput(userId, creditCard.Id, category.Id, 50m, new DateOnly(2026, 4, 9), "Farmacia"),
            CancellationToken.None);

        Assert.Equal(existingInvoice.Id, result.Id);
        Assert.Equal(150m, result.TotalAmount);
        Assert.Single(invoiceRepository.AddedInvoices, x => x.Id == existingInvoice.Id);
    }
    [Fact]
    public async Task RegisterCardExpenseAsync_ShouldSplitInstallmentsAcrossFutureInvoices()
    {
        var nowUtc = new DateTime(2026, 4, 9, 15, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var creditCard = CreditCard.Create(userId, "Cartao Azul", "Visa", 3000m, 10, 18, null, nowUtc.AddDays(-1));
        var category = TransactionCategory.CreateUserCategory(userId, "Eletronicos", TransactionCategoryType.Expense, null, null, nowUtc.AddDays(-2));
        var invoiceRepository = new FakeInvoiceRepository();
        var expenseRepository = new FakeCreditCardExpenseRepository();
        var service = CreateService(
            invoiceRepository,
            new FakeCreditCardRepository(creditCard),
            nowUtc,
            categoryRepository: new FakeTransactionCategoryRepository(category),
            expenseRepository: expenseRepository);

        var result = await service.RegisterCardExpenseAsync(
            new RegisterCardExpenseInput(userId, creditCard.Id, category.Id, 300m, new DateOnly(2026, 4, 11), "Notebook", InstallmentCount: 3),
            CancellationToken.None);

        Assert.Equal(2026, result.ReferenceYear);
        Assert.Equal(7, result.ReferenceMonth);
        Assert.Equal(3, invoiceRepository.AddedInvoices.Count);
        Assert.Equal(3, expenseRepository.AddedExpenses.Count);
        Assert.All(expenseRepository.AddedExpenses, expense => Assert.Equal(100m, expense.Amount));
        Assert.Equal([1, 2, 3], expenseRepository.AddedExpenses.OrderBy(x => x.InstallmentNumber).Select(x => x.InstallmentNumber).ToArray());
        Assert.Single(expenseRepository.AddedExpenses.Select(x => x.InstallmentGroupId).Distinct());
    }
    [Fact]
    public async Task GetCardExpensesByUserAsync_ShouldFilterDetailedPurchasesByInvoice()
    {
        var nowUtc = new DateTime(2026, 4, 9, 15, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var creditCard = CreditCard.Create(userId, "Cartao Azul", "Visa", 3000m, 10, 18, null, nowUtc.AddDays(-1));
        var category = TransactionCategory.CreateUserCategory(userId, "Mercado", TransactionCategoryType.Expense, null, null, nowUtc.AddDays(-2));
        var invoiceA = Invoice.Open(userId, creditCard.Id, 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), nowUtc.AddDays(-5));
        var invoiceB = Invoice.Open(userId, creditCard.Id, 2026, 5, new DateOnly(2026, 4, 11), new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 18), nowUtc.AddDays(-2));
        var groupId = Guid.NewGuid();
        var expenseA = CreditCardExpense.Register(userId, creditCard.Id, invoiceA.Id, category.Id, groupId, 1, 2, 80m, new DateOnly(2026, 4, 9), "Mercado A", nowUtc.AddDays(-1));
        var expenseB = CreditCardExpense.Register(userId, creditCard.Id, invoiceB.Id, category.Id, groupId, 2, 2, 120m, new DateOnly(2026, 4, 11), "Mercado B", nowUtc);
        var service = CreateService(
            new FakeInvoiceRepository(invoiceA, invoiceB),
            new FakeCreditCardRepository(creditCard),
            nowUtc,
            categoryRepository: new FakeTransactionCategoryRepository(category),
            expenseRepository: new FakeCreditCardExpenseRepository(expenseA, expenseB));

        var result = await service.GetCardExpensesByUserAsync(userId, null, invoiceA.Id, CancellationToken.None);

        var single = Assert.Single(result);
        Assert.Equal("Mercado", single.TransactionCategoryName);
        Assert.Equal(80m, single.Amount);
        Assert.Equal(invoiceA.Id, single.InvoiceId);
        Assert.Equal(1, single.InstallmentNumber);
        Assert.Equal(2, single.InstallmentCount);
    }
    [Fact]
    public async Task GetByUserAsync_ShouldMapCreditCardName()
    {
        var nowUtc = new DateTime(2026, 4, 9, 15, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var creditCard = CreditCard.Create(userId, "Cartao Azul", "Visa", 2500m, 10, 18, null, nowUtc.AddDays(-5));
        var invoice = Invoice.Open(userId, creditCard.Id, 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), nowUtc);
        var invoiceRepository = new FakeInvoiceRepository(invoice);
        var service = CreateService(invoiceRepository, new FakeCreditCardRepository(creditCard), nowUtc);

        var result = await service.GetByUserAsync(userId, null, CancellationToken.None);

        var single = Assert.Single(result);
        Assert.Equal("Cartao Azul", single.CreditCardName);
    }

    private static InvoiceService CreateService(
        FakeInvoiceRepository invoiceRepository,
        FakeCreditCardRepository creditCardRepository,
        DateTime nowUtc,
        FakeFinancialAccountRepository? financialAccountRepository = null,
        FakeTransactionCategoryRepository? categoryRepository = null,
        FakeCreditCardExpenseRepository? expenseRepository = null)
    {
        return new InvoiceService(
            invoiceRepository,
            creditCardRepository,
            financialAccountRepository ?? new FakeFinancialAccountRepository(),
            categoryRepository ?? new FakeTransactionCategoryRepository(),
            expenseRepository ?? new FakeCreditCardExpenseRepository(),
            new FakeDateTimeProvider(nowUtc));
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
            AddedInvoices = invoices.ToList();
        }

        public bool DuplicateExists { get; set; }
        public List<Invoice> AddedInvoices { get; }
        public int SaveChangesCalls { get; private set; }

        public Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
        {
            _invoices[invoice.Id] = invoice;
            if (AddedInvoices.All(existing => existing.Id != invoice.Id))
            {
                AddedInvoices.Add(invoice);
            }

            return Task.CompletedTask;
        }

        public Task<bool> ExistsByUserCreditCardAndReferenceAsync(Guid userId, Guid creditCardId, int referenceYear, int referenceMonth, CancellationToken cancellationToken)
        {
            if (DuplicateExists)
            {
                return Task.FromResult(true);
            }

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
            if (creditCardId.HasValue)
            {
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
        public Task AddAsync(FinancialAccount financialAccount, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<FinancialAccount?> GetByUserIdAndIdAsync(Guid userId, Guid financialAccountId, CancellationToken cancellationToken) => Task.FromResult<FinancialAccount?>(null);
        public Task<IReadOnlyList<FinancialAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult((IReadOnlyList<FinancialAccount>)[]);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTransactionCategoryRepository : ITransactionCategoryRepository
    {
        private readonly Dictionary<Guid, TransactionCategory> _categories;

        public FakeTransactionCategoryRepository(params TransactionCategory[] categories)
        {
            _categories = categories.ToDictionary(x => x.Id);
        }

        public Task AddAsync(TransactionCategory transactionCategory, CancellationToken cancellationToken)
        {
            _categories[transactionCategory.Id] = transactionCategory;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByUserAndNameAndTypeAsync(Guid userId, string normalizedName, TransactionCategoryType type, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<TransactionCategory?> GetByUserIdAndIdAsync(Guid userId, Guid transactionCategoryId, CancellationToken cancellationToken)
        {
            _categories.TryGetValue(transactionCategoryId, out var category);
            return Task.FromResult(category is not null && category.UserId == userId ? category : null);
        }

        public Task<IReadOnlyList<TransactionCategory>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyList<TransactionCategory>)_categories.Values.Where(x => x.UserId == userId).ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeCreditCardExpenseRepository : ICreditCardExpenseRepository
    {
        public List<CreditCardExpense> AddedExpenses { get; } = [];

        public FakeCreditCardExpenseRepository(params CreditCardExpense[] expenses)
        {
            AddedExpenses = [.. expenses];
        }

        public Task AddAsync(CreditCardExpense expense, CancellationToken cancellationToken)
        {
            AddedExpenses.Add(expense);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CreditCardExpense>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyList<CreditCardExpense>)AddedExpenses.Where(x => x.UserId == userId).ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}



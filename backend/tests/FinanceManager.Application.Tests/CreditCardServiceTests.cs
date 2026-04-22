using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.CreditCards.Contracts;
using FinanceManager.Application.CreditCards.Services;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Tests;

public sealed class CreditCardServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistCreditCardAndReturnMappedDto()
    {
        var nowUtc = new DateTime(2026, 4, 9, 11, 0, 0, DateTimeKind.Utc);
        var repository = new FakeCreditCardRepository();
        var service = CreateService(repository, nowUtc);
        var userId = Guid.NewGuid();

        var result = await service.CreateAsync(
            new CreateCreditCardInput(
                userId,
                "Cartao principal",
                "Visa",
                3000m,
                8,
                15,
                "Uso principal"),
            CancellationToken.None);

        Assert.Equal("Cartao principal", result.Name);
        Assert.Equal("Visa", result.Brand);
        Assert.Equal(3000m, result.CreditLimit);
        Assert.Single(repository.AddedCreditCards);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task GetByUserAsync_ShouldReturnOnlyAuthenticatedUserCreditCards()
    {
        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 4, 9, 11, 0, 0, DateTimeKind.Utc);
        var repository = new FakeCreditCardRepository(
            CreditCard.Create(userId, "Cartao A", null, 1000m, 5, 12, null, nowUtc),
            CreditCard.Create(anotherUserId, "Cartao B", null, 2000m, 7, 14, null, nowUtc));
        var service = CreateService(repository, nowUtc);

        var result = await service.GetByUserAsync(userId, CancellationToken.None);

        var single = Assert.Single(result);
        Assert.Equal("Cartao A", single.Name);
    }

    [Fact]
    public async Task GetOverviewByUserAsync_ShouldReturnInitialCardSituation()
    {
        var userId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 4, 9, 11, 0, 0, DateTimeKind.Utc);
        var card = CreditCard.Create(userId, "Cartao A", "Visa", 1000m, 10, 18, null, nowUtc.AddDays(-10));
        var invoice = Invoice.Open(userId, card.Id, 2026, 4, new DateOnly(2026, 3, 11), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 18), nowUtc.AddDays(-5));
        invoice.AddCharge(250m, nowUtc.AddDays(-3));
        var expense = CreditCardExpense.Register(userId, card.Id, invoice.Id, Guid.NewGuid(), Guid.NewGuid(), 1, 1, 250m, new DateOnly(2026, 4, 9), "Mercado", nowUtc.AddDays(-2));
        var service = CreateService(
            new FakeCreditCardRepository(card),
            nowUtc,
            invoiceRepository: new FakeInvoiceRepository(invoice),
            expenseRepository: new FakeCreditCardExpenseRepository(expense));

        var result = await service.GetOverviewByUserAsync(userId, CancellationToken.None);

        var single = Assert.Single(result);
        Assert.Equal(card.Id, single.CreditCardId);
        Assert.Equal(250m, single.OpenInvoiceAmount);
        Assert.Equal(1, single.OpenInvoicesCount);
        Assert.Equal(1, single.TotalInvoicesCount);
        Assert.Equal(250m, single.TotalPurchasesAmount);
        Assert.Equal(1, single.TotalPurchasesCount);
        Assert.Equal(2026, single.LatestInvoiceReferenceYear);
        Assert.Equal(4, single.LatestInvoiceReferenceMonth);
        Assert.Equal(new DateOnly(2026, 4, 9), single.LastPurchaseOn);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectNegativeLimit()
    {
        var service = CreateService(new FakeCreditCardRepository(), DateTime.UtcNow);

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(
            new CreateCreditCardInput(
                Guid.NewGuid(),
                "Cartao",
                null,
                -1m,
                5,
                12,
                null),
            CancellationToken.None));

        Assert.Equal("O limite do cartao nao pode ser negativo.", exception.Message);
    }

    private static CreditCardService CreateService(
        FakeCreditCardRepository creditCardRepository,
        DateTime nowUtc,
        FakeInvoiceRepository? invoiceRepository = null,
        FakeCreditCardExpenseRepository? expenseRepository = null)
    {
        return new CreditCardService(
            creditCardRepository,
            invoiceRepository ?? new FakeInvoiceRepository(),
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

    private sealed class FakeCreditCardRepository : ICreditCardRepository
    {
        private readonly Dictionary<Guid, CreditCard> _creditCards;

        public FakeCreditCardRepository(params CreditCard[] creditCards)
        {
            _creditCards = creditCards.ToDictionary(x => x.Id);
        }

        public List<CreditCard> AddedCreditCards { get; } = [];
        public int SaveChangesCalls { get; private set; }

        public Task AddAsync(CreditCard creditCard, CancellationToken cancellationToken)
        {
            _creditCards[creditCard.Id] = creditCard;
            AddedCreditCards.Add(creditCard);
            return Task.CompletedTask;
        }

        public Task<CreditCard?> GetByUserIdAndIdAsync(Guid userId, Guid creditCardId, CancellationToken cancellationToken)
        {
            _creditCards.TryGetValue(creditCardId, out var creditCard);
            return Task.FromResult(creditCard is not null && creditCard.UserId == userId ? creditCard : null);
        }

        public Task<IReadOnlyList<CreditCard>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            IReadOnlyList<CreditCard> creditCards = _creditCards.Values.Where(x => x.UserId == userId).OrderBy(x => x.Name).ToList();
            return Task.FromResult(creditCards);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        private readonly Dictionary<Guid, Invoice> _invoices;

        public FakeInvoiceRepository(params Invoice[] invoices)
        {
            _invoices = invoices.ToDictionary(x => x.Id);
        }

        public Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
        {
            _invoices[invoice.Id] = invoice;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByUserCreditCardAndReferenceAsync(Guid userId, Guid creditCardId, int referenceYear, int referenceMonth, CancellationToken cancellationToken)
            => Task.FromResult(_invoices.Values.Any(x => x.UserId == userId && x.CreditCardId == creditCardId && x.ReferenceYear == referenceYear && x.ReferenceMonth == referenceMonth));

        public Task<Invoice?> GetByUserAndIdAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken)
        {
            _invoices.TryGetValue(invoiceId, out var invoice);
            return Task.FromResult(invoice is not null && invoice.UserId == userId ? invoice : null);
        }

        public Task<Invoice?> GetByUserCreditCardAndReferenceAsync(Guid userId, Guid creditCardId, int referenceYear, int referenceMonth, CancellationToken cancellationToken)
            => Task.FromResult(_invoices.Values.FirstOrDefault(x => x.UserId == userId && x.CreditCardId == creditCardId && x.ReferenceYear == referenceYear && x.ReferenceMonth == referenceMonth));

        public Task<IReadOnlyList<Invoice>> GetByUserAsync(Guid userId, Guid? creditCardId, CancellationToken cancellationToken)
        {
            IEnumerable<Invoice> query = _invoices.Values.Where(x => x.UserId == userId);
            if (creditCardId.HasValue)
            {
                query = query.Where(x => x.CreditCardId == creditCardId.Value);
            }

            return Task.FromResult((IReadOnlyList<Invoice>)query.ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeCreditCardExpenseRepository : ICreditCardExpenseRepository
    {
        private readonly List<CreditCardExpense> _expenses;

        public FakeCreditCardExpenseRepository(params CreditCardExpense[] expenses)
        {
            _expenses = [.. expenses];
        }

        public Task AddAsync(CreditCardExpense expense, CancellationToken cancellationToken)
        {
            _expenses.Add(expense);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CreditCardExpense>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyList<CreditCardExpense>)_expenses.Where(x => x.UserId == userId).ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}


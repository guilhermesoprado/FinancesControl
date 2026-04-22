using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Invoices.Contracts;

public sealed record InvoiceDto(
    Guid Id,
    Guid CreditCardId,
    string CreditCardName,
    string? CreditCardBrand,
    int ReferenceYear,
    int ReferenceMonth,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly ClosingDate,
    DateOnly DueDate,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    decimal SuggestedMinimumPaymentAmount,
    decimal LateFeeAppliedAmount,
    decimal LateInterestAppliedAmount,
    decimal RevolvingInterestAppliedAmount,
    InvoiceStatus Status,
    Guid? PaidFromFinancialAccountId,
    DateTime? PaidAtUtc,
    DateTime? ClosedAtUtc,
    DateTime CreatedAtUtc);

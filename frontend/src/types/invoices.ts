export type InvoiceStatus = "open" | "closed" | "paid" | "partiallyPaid";
export type InvoiceAdjustmentType = "credit" | "discount" | "fee" | "interest" | "penalty" | "manualIncrease" | "manualDecrease";

export interface Invoice {
  id: string;
  creditCardId: string;
  creditCardName: string;
  creditCardBrand: string | null;
  referenceYear: number;
  referenceMonth: number;
  periodStart: string;
  periodEnd: string;
  closingDate: string;
  dueDate: string;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  suggestedMinimumPaymentAmount: number;
  lateFeeAppliedAmount: number;
  lateInterestAppliedAmount: number;
  revolvingInterestAppliedAmount: number;
  status: InvoiceStatus;
  paidFromFinancialAccountId: string | null;
  paidAtUtc: string | null;
  closedAtUtc: string | null;
  createdAtUtc: string;
}

export interface CreditCardExpense {
  id: string;
  creditCardId: string;
  invoiceId: string;
  transactionCategoryId: string;
  transactionCategoryName: string;
  installmentGroupId: string;
  installmentNumber: number;
  installmentCount: number;
  amount: number;
  occurredOn: string;
  description: string | null;
  createdAtUtc: string;
}

export interface CreateInvoiceInput {
  creditCardId: string;
  referenceYear: number;
  referenceMonth: number;
}

export interface PayInvoiceInput {
  financialAccountId: string;
  amount: number;
}

export interface AdjustInvoiceInput {
  adjustmentType: InvoiceAdjustmentType;
  amount: number;
}

export interface RegisterCardExpenseInput {
  creditCardId: string;
  transactionCategoryId: string;
  amount: number;
  occurredOn: string;
  description?: string;
  targetInvoiceId?: string;
  installmentCount: number;
}

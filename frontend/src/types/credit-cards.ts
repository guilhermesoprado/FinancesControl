export interface CreditCard {
  id: string;
  name: string;
  brand: string | null;
  creditLimit: number;
  closingDay: number;
  dueDay: number;
  isActive: boolean;
  description: string | null;
  createdAtUtc: string;
}

export interface CreditCardOverview {
  creditCardId: string;
  name: string;
  brand: string | null;
  creditLimit: number;
  closingDay: number;
  dueDay: number;
  isActive: boolean;
  description: string | null;
  openInvoiceAmount: number;
  openInvoicesCount: number;
  totalInvoicesCount: number;
  totalPurchasesAmount: number;
  totalPurchasesCount: number;
  latestInvoiceReferenceYear: number | null;
  latestInvoiceReferenceMonth: number | null;
  lastPurchaseOn: string | null;
  createdAtUtc: string;
}

export interface CreateCreditCardInput {
  name: string;
  brand?: string;
  creditLimit: number;
  closingDay: number;
  dueDay: number;
  description?: string;
}

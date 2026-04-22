import type { AdjustInvoiceInput, CreateInvoiceInput, CreditCardExpense, Invoice, PayInvoiceInput, RegisterCardExpenseInput } from "@/types/invoices";
import { apiRequest } from "./api-client";

export function getInvoices(creditCardId?: string): Promise<Invoice[]> {
  const query = creditCardId ? `?creditCardId=${encodeURIComponent(creditCardId)}` : "";
  return apiRequest<Invoice[]>(`/invoices${query}`);
}

export function getCardExpenses(filters?: { creditCardId?: string; invoiceId?: string }): Promise<CreditCardExpense[]> {
  const params = new URLSearchParams();
  if (filters?.creditCardId) {
    params.set("creditCardId", filters.creditCardId);
  }
  if (filters?.invoiceId) {
    params.set("invoiceId", filters.invoiceId);
  }
  const query = params.toString();
  return apiRequest<CreditCardExpense[]>(`/invoices/expenses${query ? `?${query}` : ""}`);
}

export function createInvoice(input: CreateInvoiceInput): Promise<Invoice> {
  return apiRequest<Invoice>("/invoices", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function registerCardExpense(input: RegisterCardExpenseInput): Promise<Invoice> {
  return apiRequest<Invoice>("/invoices/expenses", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function payInvoice(invoiceId: string, input: PayInvoiceInput): Promise<Invoice> {
  return apiRequest<Invoice>(`/invoices/${invoiceId}/pay`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function adjustInvoice(invoiceId: string, input: AdjustInvoiceInput): Promise<Invoice> {
  return apiRequest<Invoice>(`/invoices/${invoiceId}/adjustments`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function closeInvoice(invoiceId: string): Promise<Invoice> {
  return apiRequest<Invoice>(`/invoices/${invoiceId}/close`, {
    method: "POST",
  });
}

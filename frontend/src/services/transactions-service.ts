import { apiRequest } from "./api-client";
import type {
  CreateExpenseTransactionInput,
  CreateIncomeTransactionInput,
  CreateTransferTransactionInput,
  GetTransactionsFilters,
  Transaction,
} from "@/types/transactions";

export function getTransactions(filters: GetTransactionsFilters): Promise<Transaction[]> {
  const params = new URLSearchParams({
    from: filters.from,
    to: filters.to,
  });

  if (filters.type) {
    params.set("type", filters.type);
  }

  if (filters.financialAccountId) {
    params.set("financialAccountId", filters.financialAccountId);
  }

  return apiRequest<Transaction[]>(`/transactions?${params.toString()}`);
}

export function createIncomeTransaction(
  input: CreateIncomeTransactionInput,
): Promise<Transaction> {
  return apiRequest<Transaction>("/transactions/income", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function createExpenseTransaction(
  input: CreateExpenseTransactionInput,
): Promise<Transaction> {
  return apiRequest<Transaction>("/transactions/expense", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function createTransferTransaction(
  input: CreateTransferTransactionInput,
): Promise<Transaction> {
  return apiRequest<Transaction>("/transactions/transfer", {
    method: "POST",
    body: JSON.stringify(input),
  });
}
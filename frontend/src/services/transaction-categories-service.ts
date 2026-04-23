import type {
  CreateTransactionCategoryInput,
  TransactionCategory,
  UpdateTransactionCategoryInput,
} from "@/types/transaction-categories";
import { apiRequest } from "./api-client";

export function getTransactionCategories(): Promise<TransactionCategory[]> {
  return apiRequest<TransactionCategory[]>("/transaction-categories");
}

export function createTransactionCategory(
  input: CreateTransactionCategoryInput,
): Promise<TransactionCategory> {
  return apiRequest<TransactionCategory>("/transaction-categories", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function updateTransactionCategory(
  id: string,
  input: UpdateTransactionCategoryInput,
): Promise<TransactionCategory> {
  return apiRequest<TransactionCategory>(`/transaction-categories/${id}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export function inactivateTransactionCategory(id: string): Promise<TransactionCategory> {
  return apiRequest<TransactionCategory>(`/transaction-categories/${id}/inactivate`, {
    method: "POST",
  });
}

import type {
  CreateTransactionCategoryInput,
  TransactionCategory,
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

import type {
  CreateFinancialAccountInput,
  FinancialAccount,
  UpdateFinancialAccountInput,
} from "@/types/financial-accounts";
import { apiRequest } from "./api-client";

export function getFinancialAccounts(): Promise<FinancialAccount[]> {
  return apiRequest<FinancialAccount[]>("/financial-accounts");
}

export function createFinancialAccount(
  input: CreateFinancialAccountInput,
): Promise<FinancialAccount> {
  return apiRequest<FinancialAccount>("/financial-accounts", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function updateFinancialAccount(
  id: string,
  input: UpdateFinancialAccountInput,
): Promise<FinancialAccount> {
  return apiRequest<FinancialAccount>(`/financial-accounts/${id}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export function inactivateFinancialAccount(id: string): Promise<FinancialAccount> {
  return apiRequest<FinancialAccount>(`/financial-accounts/${id}/inactivate`, {
    method: "POST",
  });
}

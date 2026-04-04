import type {
  CreateFinancialAccountInput,
  FinancialAccount,
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

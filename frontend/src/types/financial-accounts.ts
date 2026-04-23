export type FinancialAccountType =
  | "bank_account"
  | "wallet"
  | "investment_account";

export interface FinancialAccount {
  id: string;
  name: string;
  type: FinancialAccountType;
  initialBalance: number;
  currentBalanceSnapshot: number | null;
  isActive: boolean;
  institutionName: string | null;
  description: string | null;
  createdAtUtc: string;
}

export interface CreateFinancialAccountInput {
  name: string;
  type: FinancialAccountType;
  initialBalance: number;
  institutionName?: string;
  description?: string;
}

export interface UpdateFinancialAccountInput {
  name: string;
  type: FinancialAccountType;
  institutionName?: string;
  description?: string;
}

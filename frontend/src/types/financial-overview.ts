export type FinancialOverviewAccountType =
  | "bank_account"
  | "wallet"
  | "investment_account";

export type FinancialOverviewTransactionType = "income" | "expense" | "transfer";
export type FinancialOverviewTransactionStatus = "posted" | "scheduled";

export interface FinancialOverviewAccount {
  id: string;
  name: string;
  type: FinancialOverviewAccountType;
  visibleBalance: number;
  institutionName: string | null;
  isActive: boolean;
}

export interface FinancialOverviewRecentTransaction {
  id: string;
  type: FinancialOverviewTransactionType;
  status: FinancialOverviewTransactionStatus;
  amount: number;
  occurredOn: string;
  description: string | null;
  financialAccountId: string | null;
  sourceFinancialAccountId: string | null;
  destinationFinancialAccountId: string | null;
}

export interface FinancialOverview {
  periodFrom: string;
  periodTo: string;
  consolidatedBalance: number;
  activeAccountsCount: number;
  incomeTotal: number;
  expenseTotal: number;
  transferTotal: number;
  accounts: FinancialOverviewAccount[];
  recentTransactions: FinancialOverviewRecentTransaction[];
}

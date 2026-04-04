export type TransactionType = "income" | "expense" | "transfer";

export type TransactionStatus = "posted" | "scheduled";

export interface Transaction {
  id: string;
  type: TransactionType;
  status: TransactionStatus;
  amount: number;
  occurredOn: string;
  description: string | null;
  financialAccountId: string | null;
  transactionCategoryId: string | null;
  sourceFinancialAccountId: string | null;
  destinationFinancialAccountId: string | null;
  createdAtUtc: string;
}

export interface GetTransactionsFilters {
  from: string;
  to: string;
  type?: TransactionType;
  financialAccountId?: string;
}

export interface CreateIncomeTransactionInput {
  financialAccountId: string;
  transactionCategoryId: string;
  amount: number;
  occurredOn: string;
  description?: string;
}

export interface CreateExpenseTransactionInput {
  financialAccountId: string;
  transactionCategoryId: string;
  amount: number;
  occurredOn: string;
  description?: string;
}

export interface CreateTransferTransactionInput {
  sourceFinancialAccountId: string;
  destinationFinancialAccountId: string;
  amount: number;
  occurredOn: string;
  description?: string;
}
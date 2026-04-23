export type TransactionCategoryType = "expense" | "income";

export interface TransactionCategory {
  id: string;
  name: string;
  type: TransactionCategoryType;
  color: string | null;
  icon: string | null;
  isSystem: boolean;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CreateTransactionCategoryInput {
  name: string;
  type: TransactionCategoryType;
  color?: string;
  icon?: string;
}

export interface UpdateTransactionCategoryInput {
  name: string;
  color?: string;
  icon?: string;
}

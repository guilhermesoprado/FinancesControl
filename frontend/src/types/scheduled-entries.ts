export type ScheduledEntryType = "income" | "expense";
export type ScheduledEntryPlanningMode = "oneTime" | "recurring";
export type ScheduledEntryRecurrenceFrequency = "weekly" | "monthly";
export type ScheduledEntryStatus = "scheduled" | "completed" | "skipped" | "cancelled";

export interface ScheduledEntry {
  id: string;
  financialAccountId: string;
  financialAccountName: string;
  transactionCategoryId: string;
  transactionCategoryName: string;
  type: ScheduledEntryType;
  planningMode: ScheduledEntryPlanningMode;
  recurrenceFrequency: ScheduledEntryRecurrenceFrequency | null;
  amount: number;
  description: string | null;
  startDate: string;
  nextOccurrenceDate: string | null;
  endDate: string | null;
  status: ScheduledEntryStatus;
  lastRealizedAtUtc: string | null;
  createdAtUtc: string;
}

export interface ScheduledEntryOccurrence {
  occurrenceKey: string;
  scheduledEntryId: string;
  financialAccountId: string;
  financialAccountName: string;
  transactionCategoryId: string;
  transactionCategoryName: string;
  type: ScheduledEntryType;
  planningMode: ScheduledEntryPlanningMode;
  recurrenceFrequency: ScheduledEntryRecurrenceFrequency | null;
  amount: number;
  description: string | null;
  startDate: string;
  occurrenceDate: string;
  nextOccurrenceDate: string | null;
  endDate: string | null;
  status: ScheduledEntryStatus;
  treatedAtUtc: string | null;
  canEdit: boolean;
  canAct: boolean;
  createdAtUtc: string;
}

export interface CreateScheduledEntryInput {
  financialAccountId: string;
  transactionCategoryId: string;
  planningMode: ScheduledEntryPlanningMode;
  recurrenceFrequency?: ScheduledEntryRecurrenceFrequency;
  amount: number;
  description?: string;
  startDate: string;
  endDate?: string;
}

export interface UpdateScheduledEntryInput {
  financialAccountId: string;
  transactionCategoryId: string;
  planningMode: ScheduledEntryPlanningMode;
  recurrenceFrequency?: ScheduledEntryRecurrenceFrequency;
  amount: number;
  description?: string;
  startDate: string;
  endDate?: string;
}

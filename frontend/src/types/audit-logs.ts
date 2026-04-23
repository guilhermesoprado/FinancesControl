export type AuditLogEntityType = "financial_account" | "transaction_category";

export type AuditLogAction = "created" | "updated" | "inactivated";

export interface AuditLog {
  id: string;
  entityType: AuditLogEntityType;
  entityId: string;
  action: AuditLogAction;
  summary: string;
  createdAtUtc: string;
}

export interface GetAuditLogsInput {
  entityType?: AuditLogEntityType;
  action?: AuditLogAction;
  entityId?: string;
  search?: string;
  from?: string;
  to?: string;
  limit?: number;
}

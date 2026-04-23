import type { AuditLog, GetAuditLogsInput } from "@/types/audit-logs";
import { apiRequest } from "./api-client";

export function getAuditLogs(input: GetAuditLogsInput = {}): Promise<AuditLog[]> {
  const searchParams = new URLSearchParams();

  if (input.entityType) {
    searchParams.set("entityType", input.entityType);
  }

  if (input.action) {
    searchParams.set("action", input.action);
  }

  if (input.entityId) {
    searchParams.set("entityId", input.entityId);
  }

  if (input.search) {
    searchParams.set("search", input.search);
  }

  if (input.from) {
    searchParams.set("from", input.from);
  }

  if (input.to) {
    searchParams.set("to", input.to);
  }

  if (input.limit) {
    searchParams.set("limit", String(input.limit));
  }

  const query = searchParams.toString();
  return apiRequest<AuditLog[]>(`/audit-logs${query ? `?${query}` : ""}`);
}

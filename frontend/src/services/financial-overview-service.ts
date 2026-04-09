import type { FinancialOverview } from "@/types/financial-overview";
import { apiRequest } from "./api-client";

export function getFinancialOverview(): Promise<FinancialOverview> {
  return apiRequest<FinancialOverview>("/financial-overview");
}

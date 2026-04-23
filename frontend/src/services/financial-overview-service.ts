import type { FinancialOverview } from "@/types/financial-overview";
import { apiRequest } from "./api-client";

export async function getFinancialOverview(): Promise<FinancialOverview> {
  const overview = await apiRequest<FinancialOverview>("/financial-overview");

  return {
    ...overview,
    accounts: overview.accounts ?? [],
    recentTransactions: overview.recentTransactions ?? [],
    accountSummaries: overview.accountSummaries ?? [],
    categorySummaries: overview.categorySummaries ?? [],
  };
}


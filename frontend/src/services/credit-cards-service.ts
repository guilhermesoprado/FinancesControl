import type { CreateCreditCardInput, CreditCard, CreditCardOverview } from "@/types/credit-cards";
import { apiRequest } from "./api-client";

export function getCreditCards(): Promise<CreditCard[]> {
  return apiRequest<CreditCard[]>("/credit-cards");
}

export function getCreditCardOverview(): Promise<CreditCardOverview[]> {
  return apiRequest<CreditCardOverview[]>("/credit-cards/overview");
}

export function createCreditCard(
  input: CreateCreditCardInput,
): Promise<CreditCard> {
  return apiRequest<CreditCard>("/credit-cards", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

import type {
  CreateScheduledEntryInput,
  ScheduledEntryOccurrence,
  ScheduledEntry,
  ScheduledEntryStatus,
  UpdateScheduledEntryInput,
} from "@/types/scheduled-entries";
import { apiRequest } from "./api-client";

type GetScheduledEntriesFilters = {
  status?: ScheduledEntryStatus;
  from?: string;
  to?: string;
};

export function getScheduledEntries(
  filters?: GetScheduledEntriesFilters,
): Promise<ScheduledEntryOccurrence[]> {
  const params = new URLSearchParams();

  if (filters?.status) {
    params.set("status", filters.status);
  }

  if (filters?.from) {
    params.set("from", filters.from);
  }

  if (filters?.to) {
    params.set("to", filters.to);
  }

  const query = params.toString();
  return apiRequest<ScheduledEntryOccurrence[]>(
    `/scheduled-entries${query ? `?${query}` : ""}`,
  );
}

export function createScheduledEntry(
  input: CreateScheduledEntryInput,
): Promise<ScheduledEntry> {
  return apiRequest<ScheduledEntry>("/scheduled-entries", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function updateScheduledEntry(
  id: string,
  input: UpdateScheduledEntryInput,
): Promise<ScheduledEntry> {
  return apiRequest<ScheduledEntry>(`/scheduled-entries/${id}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export function completeScheduledEntry(id: string, occurrenceDate: string): Promise<ScheduledEntry> {
  return apiRequest<ScheduledEntry>(`/scheduled-entries/${id}/complete`, {
    method: "POST",
    body: JSON.stringify({ occurrenceDate }),
  });
}

export function skipScheduledEntry(id: string, occurrenceDate: string): Promise<ScheduledEntry> {
  return apiRequest<ScheduledEntry>(`/scheduled-entries/${id}/skip`, {
    method: "POST",
    body: JSON.stringify({ occurrenceDate }),
  });
}

export function cancelScheduledEntry(id: string, occurrenceDate: string): Promise<ScheduledEntry> {
  return apiRequest<ScheduledEntry>(`/scheduled-entries/${id}/cancel`, {
    method: "POST",
    body: JSON.stringify({ occurrenceDate }),
  });
}

"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useAuth } from "@/features/auth/AuthProvider";
import { SharedSkeletonRows, SharedState } from "@/features/shared-state/SharedState";
import type { FinancialAccount } from "@/types/financial-accounts";
import type { Invoice } from "@/types/invoices";
import type { TransactionCategory } from "@/types/transaction-categories";
import type {
  CreateScheduledEntryInput,
  ScheduledEntryOccurrence,
  ScheduledEntryPlanningMode,
  ScheduledEntryRecurrenceFrequency,
  ScheduledEntryStatus,
  ScheduledEntryType,
  UpdateScheduledEntryInput,
} from "@/types/scheduled-entries";
import { ApiError } from "@/services/api-client";
import { getFinancialAccounts } from "@/services/financial-accounts-service";
import {
  cancelScheduledEntry,
  completeScheduledEntry,
  createScheduledEntry,
  getScheduledEntries,
  undoCompleteScheduledEntry,
  updateScheduledEntry,
} from "@/services/scheduled-entries-service";
import { getInvoices, payInvoice } from "@/services/invoices-service";
import { getTransactionCategories } from "@/services/transaction-categories-service";
import styles from "./ScheduledEntriesPage.module.css";

type FiltersState = {
  status: "" | ScheduledEntryStatus;
  month: string;
  from: string;
  to: string;
};

type CalendarRange = {
  from: string;
  to: string;
};

type CalendarViewMode = "month" | "week";

type PlanningDayItem =
  | {
    kind: "scheduled";
    key: string;
    date: string;
    title: string;
    subtitle: string;
    amount: number;
    type: ScheduledEntryType;
    status: ScheduledEntryStatus;
    entry: ScheduledEntryOccurrence;
  }
  | {
    kind: "invoice";
    key: string;
    date: string;
    title: string;
    subtitle: string;
    amount: number;
    type: "expense";
    status: Invoice["status"];
    invoice: Invoice;
  };

const STATUS_LABELS: Record<ScheduledEntryStatus, string> = {
  scheduled: "Agendado",
  completed: "Realizado",
  skipped: "Ignorado",
  cancelled: "Cancelado",
};

const TYPE_LABELS: Record<ScheduledEntryType, string> = {
  income: "Receita",
  expense: "Despesa",
};

const MODE_LABELS: Record<ScheduledEntryPlanningMode, string> = {
  oneTime: "Unico",
  recurring: "Recorrente",
};

const FINANCIAL_ACCOUNT_TYPE_LABELS: Record<FinancialAccount["type"], string> = {
  bank_account: "Conta bancaria",
  wallet: "Carteira",
  investment_account: "Investimento",
};

const FREQUENCY_LABELS: Record<ScheduledEntryRecurrenceFrequency, string> = {
  weekly: "Semanal",
  monthly: "Mensal",
};

function dateInput(date: Date) {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, "0");
  const day = `${date.getDate()}`.padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function monthInput(date: Date) {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, "0");
  return `${year}-${month}`;
}

function addDays(date: Date, days: number) {
  const next = new Date(date);
  next.setDate(next.getDate() + days);
  return next;
}

function buildMonthFilters(monthValue: string): FiltersState {
  const referenceDate = new Date(`${monthValue}-01T00:00:00`);
  return {
    status: "",
    month: monthValue,
    from: dateInput(startOfMonth(referenceDate)),
    to: dateInput(endOfMonth(referenceDate)),
  };
}

function createInitialFilters(): FiltersState {
  return buildMonthFilters(monthInput(new Date()));
}

function defaultSelectedDateForMonth(monthValue: string) {
  const today = new Date();
  return monthInput(today) === monthValue
    ? dateInput(today)
    : `${monthValue}-01`;
}

function createInitialForm(): CreateScheduledEntryInput {
  return {
    financialAccountId: "",
    transactionCategoryId: "",
    planningMode: "oneTime",
    recurrenceFrequency: undefined,
    amount: 0,
    description: "",
    startDate: dateInput(new Date()),
    endDate: "",
  };
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value);
}

function formatDate(value: string | null) {
  if (!value) {
    return "Sem data";
  }

  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(new Date(`${value}T00:00:00`));
}

function formatDateTime(value: string | null) {
  if (!value) {
    return "Ainda nao tratado";
  }

  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
}

function formatMonthLabel(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    month: "long",
    year: "numeric",
  }).format(new Date(`${value}-01T00:00:00`));
}

function formatReferenceMonth(year: number, month: number) {
  return `${`${month}`.padStart(2, "0")}/${year}`;
}

function startOfWeek(date: Date) {
  const next = new Date(date);
  const day = next.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  next.setDate(next.getDate() + diff);
  next.setHours(0, 0, 0, 0);
  return next;
}

function endOfWeek(date: Date) {
  const next = startOfWeek(date);
  next.setDate(next.getDate() + 6);
  return next;
}

function startOfMonth(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function endOfMonth(date: Date) {
  return new Date(date.getFullYear(), date.getMonth() + 1, 0);
}

function addDateDays(date: Date, days: number) {
  const next = new Date(date);
  next.setDate(next.getDate() + days);
  return next;
}

function addDateWeeks(date: Date, weeks: number) {
  return addDateDays(date, weeks * 7);
}

function addDateMonths(date: Date, months: number) {
  return new Date(date.getFullYear(), date.getMonth() + months, date.getDate());
}

function toDateKey(date: Date) {
  return dateInput(date);
}

function isSameDay(left: Date, right: Date) {
  return left.getFullYear() === right.getFullYear()
    && left.getMonth() === right.getMonth()
    && left.getDate() === right.getDate();
}

function formatCalendarHeader(date: Date) {
  return new Intl.DateTimeFormat("pt-BR", {
    month: "long",
    year: "numeric",
  }).format(date);
}

function formatWeekRange(start: Date, end: Date) {
  const sameMonth = start.getMonth() === end.getMonth() && start.getFullYear() === end.getFullYear();

  if (sameMonth) {
    return `${start.getDate()} - ${end.getDate()} de ${new Intl.DateTimeFormat("pt-BR", {
      month: "long",
      year: "numeric",
    }).format(start)}`;
  }

  return `${new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "short" }).format(start)} - ${new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  }).format(end)}`;
}

function daysUntil(value: string | null) {
  if (!value) {
    return null;
  }

  const today = new Date();
  const compare = new Date(`${value}T00:00:00`);
  const utcToday = Date.UTC(today.getFullYear(), today.getMonth(), today.getDate());
  const utcCompare = Date.UTC(compare.getFullYear(), compare.getMonth(), compare.getDate());
  return Math.round((utcCompare - utcToday) / 86400000);
}

function isScheduledStatus(status: ScheduledEntryStatus) {
  return status === "scheduled";
}

function minDateValue(left: string, right: string) {
  if (!left) {
    return right;
  }

  if (!right) {
    return left;
  }

  return left <= right ? left : right;
}

function maxDateValue(left: string, right: string) {
  if (!left) {
    return right;
  }

  if (!right) {
    return left;
  }

  return left >= right ? left : right;
}

function getCalendarRange(referenceDate: Date, viewMode: CalendarViewMode): CalendarRange {
  if (viewMode === "week") {
    return {
      from: toDateKey(startOfWeek(referenceDate)),
      to: toDateKey(endOfWeek(referenceDate)),
    };
  }

  const monthStart = startOfMonth(referenceDate);
  const monthEnd = endOfMonth(referenceDate);

  return {
    from: toDateKey(startOfWeek(monthStart)),
    to: toDateKey(endOfWeek(monthEnd)),
  };
}

function monthKeyFromDate(value: string) {
  return value.slice(0, 7);
}

function severityLabel(score: number) {
  if (score >= 3) {
    return "Alta";
  }

  if (score >= 1.5) {
    return "Moderada";
  }

  return "Controlada";
}

export function ScheduledEntriesPage() {
  const { logout, status } = useAuth();
  const [filters, setFilters] = useState<FiltersState>(createInitialFilters);
  const [scheduledEntries, setScheduledEntries] = useState<ScheduledEntryOccurrence[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [financialAccounts, setFinancialAccounts] = useState<FinancialAccount[]>([]);
  const [categories, setCategories] = useState<TransactionCategory[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [formState, setFormState] = useState<CreateScheduledEntryInput>(createInitialForm);
  const [editingScheduledEntryId, setEditingScheduledEntryId] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitSuccess, setSubmitSuccess] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [actionEntryId, setActionEntryId] = useState<string>("");
  const [calendarViewMode, setCalendarViewMode] = useState<CalendarViewMode>("month");
  const [calendarReferenceDate, setCalendarReferenceDate] = useState<Date>(() => new Date());
  const [selectedCalendarDate, setSelectedCalendarDate] = useState<string>(() => dateInput(new Date()));
  const [selectedCalendarPage, setSelectedCalendarPage] = useState(1);
  const [scheduledEntryPendingCancel, setScheduledEntryPendingCancel] = useState<ScheduledEntryOccurrence | null>(null);
  const [invoicePaymentTarget, setInvoicePaymentTarget] = useState<Invoice | null>(null);
  const [invoicePaymentAccountId, setInvoicePaymentAccountId] = useState("");

  const activeAccounts = useMemo(
    () => financialAccounts.filter((account) => account.isActive),
    [financialAccounts],
  );
  const activeCategories = useMemo(
    () => categories.filter((category) => category.isActive),
    [categories],
  );
  const hasOperationalBase = activeAccounts.length > 0 && activeCategories.length > 0;
  const selectedInvoicePaymentAccount = useMemo(
    () => activeAccounts.find((account) => account.id === invoicePaymentAccountId) ?? null,
    [activeAccounts, invoicePaymentAccountId],
  );

  const filteredEntries = useMemo(
    () =>
      scheduledEntries
        .filter((entry) => !filters.status || entry.status === filters.status)
        .filter((entry) => entry.status !== "cancelled")
        .filter((entry) => !filters.from || entry.occurrenceDate >= filters.from)
        .filter((entry) => !filters.to || entry.occurrenceDate <= filters.to),
    [scheduledEntries, filters],
  );

  const invoicePlanningItems = useMemo(
    () =>
      invoices
        .filter((invoice) => invoice.status === "open" || invoice.status === "partiallyPaid")
        .filter((invoice) => invoice.dueDate >= filters.from && invoice.dueDate <= filters.to)
        .map<PlanningDayItem>((invoice) => ({
          kind: "invoice",
          key: `invoice-${invoice.id}`,
          date: invoice.dueDate,
          title: `Fatura ${invoice.creditCardName}`,
          subtitle: `Cartao de credito | Ref. ${formatReferenceMonth(invoice.referenceYear, invoice.referenceMonth)}`,
          amount: invoice.remainingAmount,
          type: "expense",
          status: invoice.status,
          invoice,
        })),
    [filters.from, filters.to, invoices],
  );

  const planningItems = useMemo(
    () => {
      const scheduledItems = filteredEntries.map<PlanningDayItem>((entry) => ({
        kind: "scheduled",
        key: entry.occurrenceKey,
        date: entry.occurrenceDate,
        title: entry.description ?? entry.transactionCategoryName,
        subtitle: `${entry.transactionCategoryName} | ${entry.financialAccountName}`,
        amount: entry.amount,
        type: entry.type,
        status: entry.status,
        entry,
      }));

      return [...scheduledItems, ...invoicePlanningItems].sort((left, right) => {
        if (left.date !== right.date) {
          return left.date.localeCompare(right.date);
        }

        return left.title.localeCompare(right.title);
      });
    },
    [filteredEntries, invoicePlanningItems],
  );

  const upcomingSummary = useMemo(() => {
    const scheduled = filteredEntries.filter((entry) => entry.status === "scheduled");
    const nextSevenDays = scheduled.filter((entry) => {
      const diff = daysUntil(entry.occurrenceDate);
      return diff !== null && diff >= 0 && diff <= 7;
    });
    const recurring = scheduled.filter((entry) => entry.planningMode === "recurring");
    const totalScheduledAmount = scheduled.reduce((sum, entry) => sum + entry.amount, 0);

    return {
      scheduledCount: scheduled.length,
      nextSevenDaysCount: nextSevenDays.length,
      recurringCount: recurring.length,
      totalScheduledAmount,
    };
  }, [filteredEntries]);

  const overdueScheduledCount = useMemo(
    () =>
      filteredEntries.filter((entry) => (
        entry.status === "scheduled"
        && (daysUntil(entry.occurrenceDate) ?? 0) < 0
      )).length,
    [filteredEntries],
  );

  const operationalForecast = useMemo(() => {
    const scheduled = filteredEntries.filter((entry) => entry.status === "scheduled");
    const incomeAmount = scheduled
      .filter((entry) => entry.type === "income")
      .reduce((sum, entry) => sum + entry.amount, 0);
    const expenseAmount = scheduled
      .filter((entry) => entry.type === "expense")
      .reduce((sum, entry) => sum + entry.amount, 0);
    const netAmount = incomeAmount - expenseAmount;
    const treatedCount = filteredEntries.length - scheduled.length;
    const completionRate = filteredEntries.length === 0
      ? 0
      : Math.round((treatedCount / filteredEntries.length) * 100);

    const monthMap = scheduled.reduce<Map<string, {
      month: string;
      incomeAmount: number;
      expenseAmount: number;
      entriesCount: number;
    }>>((acc, entry) => {
      const month = monthKeyFromDate(entry.occurrenceDate);
      const current = acc.get(month) ?? {
        month,
        incomeAmount: 0,
        expenseAmount: 0,
        entriesCount: 0,
      };

      current.entriesCount += 1;
      if (entry.type === "income") {
        current.incomeAmount += entry.amount;
      } else {
        current.expenseAmount += entry.amount;
      }

      acc.set(month, current);
      return acc;
    }, new Map());

    const monthlyForecast = Array.from(monthMap.values())
      .sort((left, right) => left.month.localeCompare(right.month))
      .map((month) => ({
        ...month,
        netAmount: month.incomeAmount - month.expenseAmount,
        label: formatMonthLabel(month.month),
      }));

    const averageExpenseAmount = monthlyForecast.length === 0
      ? 0
      : monthlyForecast.reduce((sum, month) => sum + month.expenseAmount, 0) / monthlyForecast.length;
    const averageEntriesCount = monthlyForecast.length === 0
      ? 0
      : monthlyForecast.reduce((sum, month) => sum + month.entriesCount, 0) / monthlyForecast.length;

    const monthlyDecisionReading = monthlyForecast.map((month) => {
      let severityScore = 0;

      if (month.netAmount < 0) {
        severityScore += 2;
      }

      if (averageExpenseAmount > 0 && month.expenseAmount >= averageExpenseAmount * 1.2) {
        severityScore += 1;
      }

      if (averageEntriesCount > 0 && month.entriesCount >= averageEntriesCount * 1.2) {
        severityScore += 1;
      }

      return {
        ...month,
        severityScore,
        severityLabel: severityLabel(severityScore),
        loadShare: expenseAmount > 0
          ? Math.round((month.expenseAmount / expenseAmount) * 100)
          : 0,
      };
    });

    const criticalMonths = monthlyDecisionReading
      .filter((month) => month.severityScore > 0)
      .sort((left, right) => right.severityScore - left.severityScore || right.expenseAmount - left.expenseAmount);

    const peakExpenseMonth = monthlyForecast.reduce<typeof monthlyForecast[number] | null>(
      (currentPeak, month) =>
        !currentPeak || month.expenseAmount > currentPeak.expenseAmount ? month : currentPeak,
      null,
    );

    const nextDecisionMonth = monthlyDecisionReading.find((month) => month.netAmount < 0)
      ?? monthlyDecisionReading.find((month) => month.entriesCount > 0)
      ?? null;

    const operationalPriorities = [
      overdueScheduledCount > 0
        ? `Tratar ${overdueScheduledCount} item(ns) em atraso antes de abrir novas competencias.`
        : null,
      nextDecisionMonth
        ? nextDecisionMonth.netAmount < 0
          ? `${nextDecisionMonth.label} pede revisao imediata: saldo previsto negativo de ${formatCurrency(Math.abs(nextDecisionMonth.netAmount))}.`
          : `${nextDecisionMonth.label} merece acompanhamento: ${nextDecisionMonth.entriesCount} ocorrencia(s) e saldo previsto de ${formatCurrency(nextDecisionMonth.netAmount)}.`
        : null,
      peakExpenseMonth && peakExpenseMonth.expenseAmount > 0
        ? `${peakExpenseMonth.label} concentra ${expenseAmount > 0 ? Math.round((peakExpenseMonth.expenseAmount / expenseAmount) * 100) : 0}% das despesas previstas do recorte.`
        : null,
    ].filter((value): value is string => Boolean(value));

    return {
      incomeAmount,
      expenseAmount,
      netAmount,
      treatedCount,
      completionRate,
      monthlyForecast,
      monthlyDecisionReading,
      criticalMonths,
      peakExpenseMonth,
      nextDecisionMonth,
      operationalPriorities,
    };
  }, [filteredEntries, overdueScheduledCount]);

  const futureFocus = useMemo(() => {
    const scheduled = filteredEntries.filter((entry) => entry.status === "scheduled");
    const dueToday = scheduled.filter((entry) => daysUntil(entry.occurrenceDate) === 0);
    const nextThreeDays = scheduled.filter((entry) => {
      const diff = daysUntil(entry.occurrenceDate);
      return diff !== null && diff > 0 && diff <= 3;
    });
    const overdue = scheduled.filter((entry) => {
      const diff = daysUntil(entry.occurrenceDate);
      return diff !== null && diff < 0;
    });

    return {
      dueToday,
      nextThreeDays,
      overdue,
    };
  }, [filteredEntries]);

  const monthGroups = useMemo(() => {
    const grouped = filteredEntries
      .filter((entry) => entry.status === "scheduled")
      .reduce<Map<string, ScheduledEntryOccurrence[]>>((acc, entry) => {
        const monthKey = entry.occurrenceDate.slice(0, 7);
        const current = acc.get(monthKey) ?? [];
        current.push(entry);
        acc.set(monthKey, current);
        return acc;
      }, new Map());

    return Array.from(grouped.entries())
      .sort(([left], [right]) => left.localeCompare(right))
      .map(([month, entries]) => ({
        month,
        label: formatMonthLabel(month),
        entries: [...entries].sort((left, right) =>
          left.occurrenceDate.localeCompare(right.occurrenceDate),
        ),
        totalAmount: entries.reduce((sum, entry) => sum + entry.amount, 0),
      }));
  }, [filteredEntries]);

  const calendarEntriesByDate = useMemo(
    () =>
      planningItems.reduce<Map<string, PlanningDayItem[]>>((acc, entry) => {
        const current = acc.get(entry.date) ?? [];
        current.push(entry);
        acc.set(entry.date, [...current].sort((left, right) => left.amount - right.amount));
        return acc;
      }, new Map()),
    [planningItems],
  );

  const visibleCalendarDays = useMemo(() => {
    const today = new Date();
    const baseDate = new Date(calendarReferenceDate);

    if (calendarViewMode === "week") {
      const start = startOfWeek(baseDate);
      return Array.from({ length: 7 }, (_, index) => {
        const date = addDateDays(start, index);
        return {
          date,
          key: toDateKey(date),
          isCurrentMonth: true,
          isToday: isSameDay(date, today),
          isPast: date < today && !isSameDay(date, today),
          entries: calendarEntriesByDate.get(toDateKey(date)) ?? [],
        };
      });
    }

    const monthStart = startOfMonth(baseDate);
    const monthEnd = endOfMonth(baseDate);
    const gridStart = startOfWeek(monthStart);
    const gridEnd = endOfWeek(monthEnd);
    const totalDays = Math.round((gridEnd.getTime() - gridStart.getTime()) / 86400000) + 1;

    return Array.from({ length: totalDays }, (_, index) => {
      const date = addDateDays(gridStart, index);
      return {
        date,
        key: toDateKey(date),
        isCurrentMonth: date.getMonth() === baseDate.getMonth(),
        isToday: isSameDay(date, today),
        isPast: date < today && !isSameDay(date, today),
        entries: calendarEntriesByDate.get(toDateKey(date)) ?? [],
      };
    });
  }, [calendarEntriesByDate, calendarReferenceDate, calendarViewMode]);

  const calendarTitle = useMemo(() => {
    if (calendarViewMode === "week") {
      return formatWeekRange(startOfWeek(calendarReferenceDate), endOfWeek(calendarReferenceDate));
    }

    return formatCalendarHeader(calendarReferenceDate);
  }, [calendarReferenceDate, calendarViewMode]);

  const selectedCalendarEntries = useMemo(
    () => calendarEntriesByDate.get(selectedCalendarDate) ?? [],
    [calendarEntriesByDate, selectedCalendarDate],
  );

  const selectedCalendarDateLabel = useMemo(
    () => formatDate(selectedCalendarDate),
    [selectedCalendarDate],
  );

  const selectedCalendarSummary = useMemo(
    () => ({
      totalAmount: selectedCalendarEntries.reduce((sum, entry) => sum + entry.amount, 0),
      incomeAmount: selectedCalendarEntries
        .filter((entry) => entry.type === "income")
        .reduce((sum, entry) => sum + entry.amount, 0),
      expenseAmount: selectedCalendarEntries
        .filter((entry) => entry.type === "expense")
        .reduce((sum, entry) => sum + entry.amount, 0),
    }),
    [selectedCalendarEntries],
  );

  const monthlyProjection = useMemo(() => ({
    incomeAmount: planningItems
      .filter((entry) => entry.type === "income")
      .reduce((sum, entry) => sum + entry.amount, 0),
    expenseAmount: planningItems
      .filter((entry) => entry.type === "expense")
      .reduce((sum, entry) => sum + entry.amount, 0),
    incomeCount: planningItems.filter((entry) => entry.type === "income").length,
    expenseCount: planningItems.filter((entry) => entry.type === "expense").length,
  }), [planningItems]);

  const selectedCalendarTotalPages = Math.max(1, Math.ceil(selectedCalendarEntries.length / 5));

  const paginatedSelectedCalendarEntries = useMemo(() => {
    const startIndex = (selectedCalendarPage - 1) * 5;
    return selectedCalendarEntries.slice(startIndex, startIndex + 5);
  }, [selectedCalendarEntries, selectedCalendarPage]);

  useEffect(() => {
    setSelectedCalendarPage(1);
  }, [selectedCalendarDate, filters.month]);

  useEffect(() => {
    if (selectedCalendarPage > selectedCalendarTotalPages) {
      setSelectedCalendarPage(selectedCalendarTotalPages);
    }
  }, [selectedCalendarPage, selectedCalendarTotalPages]);

  const entriesByStatus = useMemo(
    () =>
      filteredEntries.reduce(
        (acc, entry) => {
          acc[entry.status] += 1;
          return acc;
        },
        {
          scheduled: 0,
          completed: 0,
          skipped: 0,
          cancelled: 0,
        },
      ),
    [filteredEntries],
  );

  const loadPageData = useCallback(
    async (
      nextFilters: FiltersState,
      nextCalendarReferenceDate: Date,
      nextCalendarViewMode: CalendarViewMode,
    ) => {
      setIsLoading(true);
      setLoadError(null);

      try {
        const calendarRange = getCalendarRange(nextCalendarReferenceDate, nextCalendarViewMode);
        const effectiveFrom = minDateValue(nextFilters.from, calendarRange.from);
        const effectiveTo = maxDateValue(nextFilters.to, calendarRange.to);
        const [accountsData, categoriesData, entriesData, invoicesData] = await Promise.all([
          getFinancialAccounts(),
          getTransactionCategories(),
          getScheduledEntries({
            status: nextFilters.status || undefined,
            from: effectiveFrom || undefined,
            to: effectiveTo || undefined,
          }),
          getInvoices(),
        ]);

        setFinancialAccounts(accountsData);
        setCategories(categoriesData);
        setScheduledEntries(entriesData);
        setInvoices(invoicesData);
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          logout();
          return;
        }

        setLoadError(
          error instanceof ApiError
            ? error.message
            : "Nao foi possivel carregar o modulo de planejamento agora.",
        );
      } finally {
        setIsLoading(false);
      }
    },
    [logout],
  );

  useEffect(() => {
    if (status !== "authenticated") {
      return;
    }

    const nextFilters = createInitialFilters();
    const nextReferenceDate = new Date(`${nextFilters.month}-01T00:00:00`);
    setFilters(nextFilters);
    setCalendarReferenceDate(nextReferenceDate);
    setCalendarViewMode("month");
    setSelectedCalendarDate(defaultSelectedDateForMonth(nextFilters.month));
    void loadPageData(nextFilters, nextReferenceDate, "month");
  }, [status, loadPageData]);

  function openModal() {
    setSubmitError(null);
    setSubmitSuccess(null);
    setFormState(createInitialForm());
    setEditingScheduledEntryId(null);
    setIsModalOpen(true);
  }

  function openModalForDate(dateValue: string) {
    setSubmitError(null);
    setSubmitSuccess(null);
    setFormState({
      ...createInitialForm(),
      startDate: dateValue,
    });
    setEditingScheduledEntryId(null);
    setSelectedCalendarDate(dateValue);
    setIsModalOpen(true);
  }

  function openEditModal(entry: ScheduledEntryOccurrence) {
    setSubmitError(null);
    setSubmitSuccess(null);
    setEditingScheduledEntryId(entry.scheduledEntryId);
    setSelectedCalendarDate(entry.occurrenceDate);
    setFormState({
      financialAccountId: entry.financialAccountId,
      transactionCategoryId: entry.transactionCategoryId,
      planningMode: entry.planningMode,
      recurrenceFrequency: entry.recurrenceFrequency ?? undefined,
      amount: entry.amount,
      description: entry.description ?? "",
      startDate: entry.startDate,
      endDate: entry.endDate ?? "",
    });
    setIsModalOpen(true);
  }

  function closeModal() {
    if (isSubmitting) {
      return;
    }

    setIsModalOpen(false);
    setEditingScheduledEntryId(null);
    setSubmitError(null);
  }

  function handleFormChange<K extends keyof CreateScheduledEntryInput>(
    field: K,
    value: CreateScheduledEntryInput[K],
  ) {
    setFormState((current) => ({
      ...current,
      [field]: value,
    }));
  }

  function updateCalendarView(nextReferenceDate: Date) {
    const monthValue = monthInput(nextReferenceDate);
    const nextFilters = buildMonthFilters(monthValue);

    setFilters(nextFilters);
    setCalendarReferenceDate(nextReferenceDate);
    setCalendarViewMode("month");
    setSelectedCalendarDate(defaultSelectedDateForMonth(monthValue));
    void loadPageData(nextFilters, nextReferenceDate, "month");
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitError(null);
    setSubmitSuccess(null);

    if (!formState.financialAccountId) {
      setSubmitError("Selecione a conta financeira do previsto.");
      return;
    }

    if (!formState.transactionCategoryId) {
      setSubmitError("Selecione a categoria do previsto.");
      return;
    }

    if (formState.amount <= 0) {
      setSubmitError("Informe um valor maior que zero para o previsto.");
      return;
    }

    if (!formState.startDate) {
      setSubmitError("Informe a data inicial do previsto.");
      return;
    }

    if (formState.planningMode === "recurring" && !formState.recurrenceFrequency) {
      setSubmitError("Selecione a frequencia da recorrencia.");
      return;
    }

    if (
      formState.endDate
      && formState.startDate
      && formState.endDate < formState.startDate
    ) {
      setSubmitError("A data final nao pode ser menor que a data inicial.");
      return;
    }

    setIsSubmitting(true);

    try {
      const payload: UpdateScheduledEntryInput = {
        ...formState,
        recurrenceFrequency:
          formState.planningMode === "recurring"
            ? formState.recurrenceFrequency
            : undefined,
        description: formState.description?.trim() || undefined,
        endDate: formState.endDate || undefined,
      };

      if (editingScheduledEntryId) {
        await updateScheduledEntry(editingScheduledEntryId, payload);
        setSubmitSuccess("Previsto atualizado com sucesso.");
      } else {
        await createScheduledEntry(payload);
        setSubmitSuccess("Previsto criado com sucesso.");
      }

      setIsModalOpen(false);
      setEditingScheduledEntryId(null);
      await loadPageData(filters, calendarReferenceDate, calendarViewMode);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      setSubmitError(
        error instanceof ApiError
          ? error.message
          : "Nao foi possivel criar o previsto agora.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleEntryAction(
    entry: ScheduledEntryOccurrence,
    action: "complete" | "undo-complete" | "cancel",
  ) {
    setSubmitError(null);
    setSubmitSuccess(null);
    setActionEntryId(entry.occurrenceKey);

    try {
      if (action === "complete") {
        await completeScheduledEntry(entry.scheduledEntryId, entry.occurrenceDate);
        setSubmitSuccess("Previsto tratado como realizado com sucesso.");
      }

      if (action === "undo-complete") {
        await undoCompleteScheduledEntry(entry.scheduledEntryId, entry.occurrenceDate);
        setSubmitSuccess("Realizado desfeito com sucesso.");
      }

      if (action === "cancel") {
        await cancelScheduledEntry(entry.scheduledEntryId, entry.occurrenceDate);
        setSubmitSuccess("Previsto cancelado com sucesso.");
      }

      await loadPageData(filters, calendarReferenceDate, calendarViewMode);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      setSubmitError(
        error instanceof ApiError
          ? error.message
          : "Nao foi possivel atualizar o previsto agora.",
      );
    } finally {
      setActionEntryId("");
    }
  }

  function handleInvoiceEdit(invoiceId: string) {
    window.location.href = `/invoices?invoiceId=${invoiceId}`;
  }

  function handleOpenInvoicePayment(invoice: Invoice) {
    setSubmitError(null);
    setSubmitSuccess(null);
    setInvoicePaymentTarget(invoice);
    setInvoicePaymentAccountId(activeAccounts[0]?.id ?? "");
  }

  function closeInvoicePaymentModal() {
    setInvoicePaymentTarget(null);
    setInvoicePaymentAccountId("");
  }

  async function handleConfirmInvoicePayment() {
    if (!invoicePaymentTarget) {
      return;
    }

    if (!invoicePaymentAccountId) {
      setSubmitError("Selecione a conta que sera usada no pagamento da fatura.");
      return;
    }

    setIsSubmitting(true);
    setSubmitError(null);
    setSubmitSuccess(null);

    try {
      await payInvoice(invoicePaymentTarget.id, {
        financialAccountId: invoicePaymentAccountId,
        amount: invoicePaymentTarget.remainingAmount,
      });

      closeInvoicePaymentModal();
      setSubmitSuccess("Fatura paga com sucesso.");
      await loadPageData(filters, calendarReferenceDate, calendarViewMode);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      setSubmitError(
        error instanceof ApiError
          ? error.message
          : "Nao foi possivel pagar a fatura agora.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className={styles.main}>
      <header className={styles.header}>
        <div>
          <p className={styles.eyebrow}>Planejamento Financeiro</p>
          <h1>Planejamento financeiro</h1>
        </div>

        <div className={styles.headerActions}>
          <button
            className={styles.primaryButton}
            onClick={openModal}
            disabled={!hasOperationalBase}
          >
            Novo previsto
          </button>
        </div>
      </header>

      <section className={styles.summaryGrid}>
        <article className={styles.summaryCard}>
          <div className={styles.summaryCardHeader}>
            <span>Projecao de receitas</span>
            <span className={styles.summaryAccent}>{formatMonthLabel(filters.month)}</span>
          </div>
          <strong className={styles.forecastPositive}>
            {formatCurrency(monthlyProjection.incomeAmount)}
          </strong>
          <small>{monthlyProjection.incomeCount} previsto(s) de receita no mes selecionado</small>
        </article>
        <article className={styles.summaryCard}>
          <div className={styles.summaryCardHeader}>
            <span>Projecao de despesas</span>
            <span className={styles.summaryAccent}>{formatMonthLabel(filters.month)}</span>
          </div>
          <strong className={styles.forecastNegative}>
            {formatCurrency(monthlyProjection.expenseAmount)}
          </strong>
          <small>{monthlyProjection.expenseCount} previsto(s) de despesa no mes selecionado</small>
        </article>
      </section>

      {submitSuccess ? (
        <div className={styles.feedbackSuccess}>{submitSuccess}</div>
      ) : null}
      {submitError ? <div className={styles.feedbackError}>{submitError}</div> : null}

      {!activeAccounts.length ? (
        <SharedState
          eyebrow="Base obrigatoria"
          title="Voce precisa de uma conta ativa para planejar"
          description="O modulo de planejamento depende de pelo menos uma conta financeira ativa no modulo de Contas."
          tone="empty"
        />
      ) : null}

      {activeAccounts.length > 0 && !activeCategories.length ? (
        <SharedState
          eyebrow="Dependencia"
          title="Planejamento exige categorias ativas"
          description="Cadastre ou reative categorias de receita e despesa para iniciar os previstos."
          tone="warning"
          compact
        />
      ) : null}

      <section className={styles.filtersCard}>
        <div className={styles.filtersHeader}>
          <div>
            <h2>Filtro mensal</h2>
          </div>
        </div>
        <div className={styles.toolbarActions}>
          <label className={styles.toolbarField}>
            <span>Mes de projecao</span>
            <input
              className={styles.toolbarInput}
              type="month"
              value={filters.month}
              onChange={(event) => {
                const nextFilters = buildMonthFilters(event.target.value);
                const nextReferenceDate = new Date(`${nextFilters.month}-01T00:00:00`);

                setFilters(nextFilters);
                setCalendarReferenceDate(nextReferenceDate);
                setCalendarViewMode("month");
                setSelectedCalendarDate(defaultSelectedDateForMonth(nextFilters.month));
                void loadPageData(nextFilters, nextReferenceDate, "month");
              }}
            />
          </label>
        </div>
      </section>

      {status === "loading" || isLoading ? <SharedSkeletonRows rows={3} /> : null}

      {status !== "loading" && !isLoading && loadError ? (
        <SharedState
          eyebrow="Planejamento"
          title="Nao foi possivel carregar seu planejamento"
          description={loadError}
          tone="error"
          compact
          actions={
            <button
              className={styles.secondaryButton}
              onClick={() => void loadPageData(filters, calendarReferenceDate, calendarViewMode)}
            >
              Tentar novamente
            </button>
          }
        />
      ) : null}

      {status !== "loading" && !isLoading && !loadError ? (
        <section className={styles.calendarCard}>
          <div className={styles.calendarHeader}>
            <div>
              <p className={styles.calendarEyebrow}>Calendario mensal</p>
              <h2>{calendarTitle}</h2>
            </div>

            <div className={styles.calendarNav}>
              <button
                className={styles.secondaryButton}
                type="button"
                onClick={() => updateCalendarView(addDateMonths(calendarReferenceDate, -1))}
              >
                Anterior
              </button>
              <button
                className={styles.secondaryButton}
                type="button"
                onClick={() => updateCalendarView(new Date())}
              >
                Hoje
              </button>
              <button
                className={styles.secondaryButton}
                type="button"
                onClick={() => updateCalendarView(addDateMonths(calendarReferenceDate, 1))}
              >
                Proximo
              </button>
            </div>
          </div>

          <div className={styles.calendarWorkspace}>
            <div className={styles.calendarBoard}>
              <div className={styles.calendarWeekdayRow}>
                {["Seg", "Ter", "Qua", "Qui", "Sex", "Sab", "Dom"].map((weekday) => (
                  <span key={weekday}>{weekday}</span>
                ))}
              </div>

                <div className={styles.calendarGridMonth}>
                  {visibleCalendarDays.map((day) => (
                    <button
                      type="button"
                      className={`${styles.calendarDay} ${day.isToday ? styles.calendarDayToday : ""} ${day.isPast ? styles.calendarDayPast : ""} ${!day.isCurrentMonth ? styles.calendarDayMuted : ""} ${selectedCalendarDate === day.key ? styles.calendarDaySelected : ""}`}
                      key={day.key}
                      onClick={() => setSelectedCalendarDate(day.key)}
                    >
                      <div className={styles.calendarDayHeader}>
                        <strong>{day.date.getDate().toString().padStart(2, "0")}</strong>
                        <span>{day.entries.length ? "" : "Livre"}</span>
                      </div>

                      <div className={styles.calendarDayCounters}>
                        {day.entries.filter((entry) => entry.type === "income").length > 0 ? (
                          <div className={`${styles.calendarCountChip} ${styles.calendarCountIncome} ${day.isPast ? styles.calendarCountIncomePast : ""}`}>
                            <span>Receitas</span>
                            <strong>{day.entries.filter((entry) => entry.type === "income").length}</strong>
                          </div>
                        ) : null}
                        {day.entries.filter((entry) => entry.type === "expense").length > 0 ? (
                          <div className={`${styles.calendarCountChip} ${styles.calendarCountExpense} ${day.isPast ? styles.calendarCountExpensePast : ""}`}>
                            <span>Despesas</span>
                            <strong>{day.entries.filter((entry) => entry.type === "expense").length}</strong>
                          </div>
                        ) : null}
                      </div>
                    </button>
                  ))}
              </div>
            </div>

            <aside className={styles.calendarSidebar}>
              <div className={styles.calendarDrawer}>
                <div className={styles.calendarDrawerHeader}>
                  <div>
                    <span className={styles.calendarEyebrow}>Previsoes do dia</span>
                    <h3>{selectedCalendarDateLabel}</h3>
                  </div>
                  <div className={styles.calendarDrawerStatus}>
                    {selectedCalendarEntries.length} item(ns)
                  </div>
                </div>

                <div className={styles.calendarDrawerSummary}>
                  <div className={styles.calendarDrawerMetric}>
                    <span>Entradas</span>
                    <strong>{formatCurrency(selectedCalendarSummary.incomeAmount)}</strong>
                  </div>
                  <div className={styles.calendarDrawerMetric}>
                    <span>Saidas</span>
                    <strong>{formatCurrency(selectedCalendarSummary.expenseAmount)}</strong>
                  </div>
                </div>

                {selectedCalendarEntries.length > 0 ? (
                  <>
                    <div className={styles.calendarDrawerList}>
                      {paginatedSelectedCalendarEntries.map((entry) => (
                        <article
                          className={`${styles.calendarDrawerItem} ${
                            entry.kind === "scheduled" && entry.status === "completed"
                              ? styles.calendarDrawerItemCompleted
                              : ""
                          }`}
                          key={entry.key}
                        >
                          <div className={styles.calendarDrawerItemTop}>
                            <div>
                              <div className={styles.calendarDrawerItemTitleRow}>
                                <h4>{entry.title}</h4>
                                {entry.kind === "scheduled" && entry.status === "completed" ? (
                                  <span className={styles.realizedBadge}>Realizado</span>
                                ) : null}
                                {entry.kind === "invoice" ? (
                                  <span className={styles.invoiceBadge}>Fatura</span>
                                ) : null}
                              </div>
                              <p>{entry.subtitle}</p>
                            </div>
                            <strong className={entry.type === "income" ? styles.forecastPositive : styles.forecastNegative}>
                              {entry.type === "income" ? "+ " : "- "}
                              {formatCurrency(entry.amount)}
                            </strong>
                          </div>
                          <div className={styles.calendarDrawerActionsCompact}>
                            {entry.kind === "invoice" ? (
                              <>
                                <button
                                  className={styles.primaryGhostButton}
                                  type="button"
                                  onClick={() => handleOpenInvoicePayment(entry.invoice)}
                                  disabled={isSubmitting || activeAccounts.length === 0}
                                >
                                  Realizar
                                </button>
                                <button
                                  className={styles.secondaryButton}
                                  type="button"
                                  onClick={() => handleInvoiceEdit(entry.invoice.id)}
                                  disabled={isSubmitting}
                                >
                                  Editar
                                </button>
                              </>
                            ) : entry.status === "completed" ? (
                              <>
                                <button
                                  className={styles.primaryGhostButton}
                                  type="button"
                                  onClick={() => void handleEntryAction(entry.entry, "undo-complete")}
                                  disabled={actionEntryId === entry.entry.occurrenceKey}
                                >
                                  {actionEntryId === entry.entry.occurrenceKey ? "Salvando..." : "Desfazer"}
                                </button>
                                <button
                                  className={styles.secondaryButton}
                                  type="button"
                                  onClick={() => openEditModal(entry.entry)}
                                  disabled={!entry.entry.canEdit || actionEntryId === entry.entry.occurrenceKey}
                                >
                                  Editar
                                </button>
                              </>
                            ) : (
                              <>
                                <button
                                  className={styles.primaryGhostButton}
                                  type="button"
                                  onClick={() => void handleEntryAction(entry.entry, "complete")}
                                  disabled={!entry.entry.canAct || actionEntryId === entry.entry.occurrenceKey}
                                >
                                  {actionEntryId === entry.entry.occurrenceKey ? "Salvando..." : "Realizar"}
                                </button>
                                <button
                                  className={styles.secondaryButton}
                                  type="button"
                                  onClick={() => openEditModal(entry.entry)}
                                  disabled={!entry.entry.canEdit || actionEntryId === entry.entry.occurrenceKey}
                                >
                                  Editar
                                </button>
                                <button
                                  className={styles.dangerButton}
                                  type="button"
                                  onClick={() => setScheduledEntryPendingCancel(entry.entry)}
                                  disabled={!entry.entry.canAct || actionEntryId === entry.entry.occurrenceKey}
                                >
                                  Cancelar
                                </button>
                              </>
                            )}
                          </div>
                        </article>
                      ))}
                    </div>

                    {selectedCalendarEntries.length > 5 ? (
                      <div className={styles.pagination}>
                        <button
                          className={styles.secondaryButton}
                          type="button"
                          onClick={() => setSelectedCalendarPage((current) => Math.max(1, current - 1))}
                          disabled={selectedCalendarPage === 1}
                        >
                          Anterior
                        </button>
                        <span>
                          Pagina {selectedCalendarPage} de {selectedCalendarTotalPages}
                        </span>
                        <button
                          className={styles.secondaryButton}
                          type="button"
                          onClick={() => setSelectedCalendarPage((current) => Math.min(selectedCalendarTotalPages, current + 1))}
                          disabled={selectedCalendarPage === selectedCalendarTotalPages}
                        >
                          Proxima
                        </button>
                      </div>
                    ) : null}
                  </>
                ) : (
                  <div className={styles.calendarDrawerEmpty}>
                    Nenhuma previsao para esta data. Use o botao abaixo para criar um novo previsto neste dia.
                  </div>
                )}

                <button
                  className={styles.primaryButton}
                  type="button"
                  onClick={() => openModalForDate(selectedCalendarDate)}
                  disabled={!hasOperationalBase}
                >
                  Adicionar evento para esta data
                </button>
              </div>
            </aside>
          </div>
        </section>
      ) : null}

      {scheduledEntryPendingCancel ? (
        <div className={styles.modalOverlay} role="presentation">
          <div className={styles.modalCard}>
            <div className={styles.modalHeader}>
              <div>
                <p className={styles.eyebrow}>Cancelar previsto</p>
                <h2>Confirmar cancelamento</h2>
                <p>
                  Ao cancelar este previsto, a acao nao podera ser desfeita. O item sumira da tela e, caso tenha sido cancelado por engano, sera necessario criar outro previsto.
                </p>
              </div>
              <button className={styles.iconButton} onClick={() => setScheduledEntryPendingCancel(null)}>
                Fechar
              </button>
            </div>

            <div className={styles.form}>
              <div className={styles.readonlyCard}>
                <strong>{scheduledEntryPendingCancel.description ?? scheduledEntryPendingCancel.transactionCategoryName}</strong>
                <small>{`${scheduledEntryPendingCancel.transactionCategoryName} | ${scheduledEntryPendingCancel.financialAccountName} | ${formatDate(scheduledEntryPendingCancel.occurrenceDate)}`}</small>
              </div>

              <div className={styles.formActions}>
                <button
                  className={styles.secondaryButton}
                  type="button"
                  onClick={() => setScheduledEntryPendingCancel(null)}
                >
                  Voltar
                </button>
                <button
                  className={styles.dangerButton}
                  type="button"
                  onClick={async () => {
                    await handleEntryAction(scheduledEntryPendingCancel, "cancel");
                    setScheduledEntryPendingCancel(null);
                  }}
                  disabled={actionEntryId === scheduledEntryPendingCancel.occurrenceKey}
                >
                  {actionEntryId === scheduledEntryPendingCancel.occurrenceKey ? "Cancelando..." : "Confirmar cancelamento"}
                </button>
              </div>
            </div>
          </div>
        </div>
      ) : null}

      {invoicePaymentTarget ? (
        <div className={styles.modalOverlay} role="presentation">
          <div className={`${styles.modalCard} ${styles.paymentModalCard}`}>
            <div className={styles.modalHeader}>
              <div>
                <p className={styles.eyebrow}>Pagamento de fatura</p>
                <h2>Selecionar conta para pagar</h2>
                <p>Escolha a conta financeira que sera usada para quitar esta fatura diretamente do Planejamento.</p>
              </div>
              <button className={styles.iconButton} onClick={closeInvoicePaymentModal}>
                Fechar
              </button>
            </div>

            <div className={styles.form}>
              <div className={styles.paymentHero}>
                <div className={`${styles.readonlyCard} ${styles.paymentHeroCard}`}>
                  <span className={styles.paymentCardLabel}>Cartao</span>
                  <strong>{invoicePaymentTarget.creditCardName}</strong>
                  <small>{`Fatura ${formatReferenceMonth(invoicePaymentTarget.referenceYear, invoicePaymentTarget.referenceMonth)}`}</small>
                </div>
                <div className={`${styles.readonlyCard} ${styles.paymentHeroCard}`}>
                  <span className={styles.paymentCardLabel}>Vencimento</span>
                  <strong>{formatDate(invoicePaymentTarget.dueDate)}</strong>
                  <small>Pagamento tratado no modulo de Faturas.</small>
                </div>
                <div className={`${styles.readonlyCard} ${styles.paymentHeroCard} ${styles.paymentHeroAccent}`}>
                  <span className={styles.paymentCardLabel}>Valor a pagar</span>
                  <strong>{formatCurrency(invoicePaymentTarget.remainingAmount)}</strong>
                  <small>Saldo remanescente que sera baixado ao confirmar.</small>
                </div>
              </div>

              <div className={styles.paymentAccountSection}>
                <div className={styles.paymentSectionHeader}>
                  <div>
                    <span className={styles.paymentSectionEyebrow}>Conta de pagamento</span>
                    <h3>Escolha de onde a saida sera registrada</h3>
                  </div>
                  {selectedInvoicePaymentAccount ? (
                    <div className={styles.paymentSelectedPill}>
                      {selectedInvoicePaymentAccount.name}
                    </div>
                  ) : null}
                </div>

                <div className={styles.paymentAccountGrid}>
                  {activeAccounts.map((account) => (
                    <button
                      key={account.id}
                      className={`${styles.paymentAccountCard} ${
                        invoicePaymentAccountId === account.id ? styles.paymentAccountCardActive : ""
                      }`}
                      type="button"
                      onClick={() => setInvoicePaymentAccountId(account.id)}
                    >
                      <div className={styles.paymentAccountCardTop}>
                        <div>
                          <strong>{account.name}</strong>
                          <small>{account.institutionName || FINANCIAL_ACCOUNT_TYPE_LABELS[account.type]}</small>
                        </div>
                        <span className={styles.paymentAccountBadge}>
                          {invoicePaymentAccountId === account.id ? "Selecionada" : "Conta"}
                        </span>
                      </div>
                      <div className={styles.paymentAccountMeta}>
                        <span>{FINANCIAL_ACCOUNT_TYPE_LABELS[account.type]}</span>
                        <strong>{formatCurrency(account.currentBalanceSnapshot ?? account.initialBalance)}</strong>
                      </div>
                    </button>
                  ))}
                </div>

                {activeAccounts.length === 0 ? (
                  <div className={styles.calendarDrawerEmpty}>
                    Nenhuma conta ativa disponivel para registrar o pagamento desta fatura.
                  </div>
                ) : null}
              </div>

              <div className={styles.paymentSummaryPanel}>
                <div className={styles.paymentSummaryRow}>
                  <span>Origem</span>
                  <strong>{invoicePaymentTarget.creditCardName}</strong>
                </div>
                <div className={styles.paymentSummaryRow}>
                  <span>Conta escolhida</span>
                  <strong>{selectedInvoicePaymentAccount?.name ?? "Selecione uma conta"}</strong>
                </div>
                <div className={styles.paymentSummaryRow}>
                  <span>Valor da baixa</span>
                  <strong>{formatCurrency(invoicePaymentTarget.remainingAmount)}</strong>
                </div>
              </div>

              {submitError ? <div className={styles.feedbackError}>{submitError}</div> : null}

              <div className={styles.formActions}>
                <button
                  className={styles.secondaryButton}
                  type="button"
                  onClick={closeInvoicePaymentModal}
                >
                  Voltar
                </button>
                <button
                  className={styles.primaryButton}
                  type="button"
                  onClick={() => void handleConfirmInvoicePayment()}
                  disabled={isSubmitting || !invoicePaymentAccountId}
                >
                  {isSubmitting ? "Pagando..." : "Confirmar pagamento"}
                </button>
              </div>
            </div>
          </div>
        </div>
      ) : null}

      {isModalOpen ? (
        <div className={styles.modalOverlay} role="presentation">
          <div className={styles.modalCard}>
            <div className={styles.modalHeader}>
              <div>
                <p className={styles.eyebrow}>{editingScheduledEntryId ? "Editar previsto" : "Novo previsto"}</p>
                <h2>{editingScheduledEntryId ? "Editar lancamento planejado" : "Criar lancamento planejado"}</h2>
                <p>
                  {editingScheduledEntryId
                    ? "Atualize conta, categoria, recorrencia e datas sem sair do fluxo operacional do planejamento."
                    : "Cadastre um compromisso futuro unico ou recorrente mantendo ownership explicito de conta, categoria, valor e data."}
                </p>
              </div>
              <button className={styles.iconButton} onClick={closeModal}>
                Fechar
              </button>
            </div>

            <form className={styles.form} onSubmit={handleSubmit}>
              <div className={styles.fieldRow}>
                <label className={styles.field}>
                  <span>Conta</span>
                  <select
                    value={formState.financialAccountId}
                    onChange={(event) =>
                      handleFormChange("financialAccountId", event.target.value)
                    }
                  >
                    <option value="">Selecione uma conta</option>
                    {activeAccounts.map((account) => (
                      <option key={account.id} value={account.id}>
                        {account.name}
                      </option>
                    ))}
                  </select>
                </label>

                <label className={styles.field}>
                  <span>Categoria</span>
                  <select
                    value={formState.transactionCategoryId}
                    onChange={(event) =>
                      handleFormChange("transactionCategoryId", event.target.value)
                    }
                  >
                    <option value="">Selecione uma categoria</option>
                    {activeCategories.map((category) => (
                      <option key={category.id} value={category.id}>
                        {category.name} | {TYPE_LABELS[category.type]}
                      </option>
                    ))}
                  </select>
                </label>
              </div>

              <div className={styles.fieldRow}>
                <label className={styles.field}>
                  <span>Modo</span>
                  <select
                    value={formState.planningMode}
                    onChange={(event) =>
                      handleFormChange(
                        "planningMode",
                        event.target.value as ScheduledEntryPlanningMode,
                      )
                    }
                  >
                    <option value="oneTime">Unico</option>
                    <option value="recurring">Recorrente</option>
                  </select>
                </label>

                <label className={styles.field}>
                  <span>Frequencia</span>
                  <select
                    value={formState.recurrenceFrequency ?? ""}
                    onChange={(event) =>
                      handleFormChange(
                        "recurrenceFrequency",
                        (event.target.value || undefined) as CreateScheduledEntryInput["recurrenceFrequency"],
                      )
                    }
                    disabled={formState.planningMode !== "recurring"}
                  >
                    <option value="">Selecione</option>
                    <option value="weekly">Semanal</option>
                    <option value="monthly">Mensal</option>
                  </select>
                </label>
              </div>

              <div className={styles.fieldRow}>
                <label className={styles.field}>
                  <span>Valor</span>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={formState.amount}
                    onChange={(event) =>
                      handleFormChange("amount", Number(event.target.value || 0))
                    }
                  />
                </label>

                <label className={styles.field}>
                  <span>Data inicial</span>
                  <input
                    type="date"
                    value={formState.startDate}
                    onChange={(event) =>
                      handleFormChange("startDate", event.target.value)
                    }
                  />
                </label>
              </div>

              <div className={styles.fieldRow}>
                <label className={styles.field}>
                  <span>Data final</span>
                  <input
                    type="date"
                    value={formState.endDate ?? ""}
                    onChange={(event) =>
                      handleFormChange("endDate", event.target.value)
                    }
                    disabled={formState.planningMode !== "recurring"}
                  />
                </label>

                <label className={styles.field}>
                  <span>Leitura esperada</span>
                  <div className={styles.readonlyCard}>
                    <strong>
                      {formState.planningMode === "recurring"
                        ? "Recorrencia controlada"
                        : "Compromisso unico"}
                    </strong>
                    <small>
                      {formState.planningMode === "recurring"
                        ? "A cada tratamento o backend avanca a proxima ocorrencia."
                        : "Ao tratar este item, ele sai da fila ativa."}
                    </small>
                  </div>
                </label>
              </div>

              <label className={styles.field}>
                <span>Descricao</span>
                <textarea
                  rows={4}
                  value={formState.description ?? ""}
                  onChange={(event) =>
                    handleFormChange("description", event.target.value)
                  }
                  placeholder="Conta de internet, assinatura, salario previsto..."
                />
              </label>

              {submitError ? <div className={styles.feedbackError}>{submitError}</div> : null}

              <div className={styles.formActions}>
                <button
                  className={styles.secondaryButton}
                  type="button"
                  onClick={closeModal}
                  disabled={isSubmitting}
                >
                  Cancelar
                </button>
                <button className={styles.primaryButton} type="submit" disabled={isSubmitting}>
                  {isSubmitting
                    ? editingScheduledEntryId
                      ? "Salvando..."
                      : "Criando..."
                    : editingScheduledEntryId
                      ? "Salvar edicao"
                      : "Criar previsto"}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}
    </main>
  );
}

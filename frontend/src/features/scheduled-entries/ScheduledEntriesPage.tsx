"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useAuth } from "@/features/auth/AuthProvider";
import { SharedSkeletonRows, SharedState } from "@/features/shared-state/SharedState";
import type { FinancialAccount } from "@/types/financial-accounts";
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
  skipScheduledEntry,
  updateScheduledEntry,
} from "@/services/scheduled-entries-service";
import { getTransactionCategories } from "@/services/transaction-categories-service";
import styles from "./ScheduledEntriesPage.module.css";

type FiltersState = {
  status: "" | ScheduledEntryStatus;
  from: string;
  to: string;
};

type CalendarRange = {
  from: string;
  to: string;
};

type CalendarViewMode = "month" | "week";

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

function addDays(date: Date, days: number) {
  const next = new Date(date);
  next.setDate(next.getDate() + days);
  return next;
}

function createInitialFilters(): FiltersState {
  const now = new Date();
  return {
    status: "scheduled",
    from: dateInput(now),
    to: dateInput(addDays(now, 90)),
  };
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
  const { logout, status, user } = useAuth();
  const [filters, setFilters] = useState<FiltersState>(createInitialFilters);
  const [scheduledEntries, setScheduledEntries] = useState<ScheduledEntryOccurrence[]>([]);
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

  const activeAccounts = useMemo(
    () => financialAccounts.filter((account) => account.isActive),
    [financialAccounts],
  );
  const activeCategories = useMemo(
    () => categories.filter((category) => category.isActive),
    [categories],
  );
  const hasOperationalBase = activeAccounts.length > 0 && activeCategories.length > 0;

  const filteredEntries = useMemo(
    () =>
      scheduledEntries
        .filter((entry) => !filters.status || entry.status === filters.status)
        .filter((entry) => !filters.from || entry.occurrenceDate >= filters.from)
        .filter((entry) => !filters.to || entry.occurrenceDate <= filters.to),
    [scheduledEntries, filters],
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
      scheduledEntries
        .reduce<Map<string, ScheduledEntryOccurrence[]>>((acc, entry) => {
          const current = acc.get(entry.occurrenceDate) ?? [];
          current.push(entry);
          acc.set(entry.occurrenceDate, [...current].sort((left, right) => left.amount - right.amount));
          return acc;
        }, new Map()),
    [scheduledEntries],
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
      nextCalendarReferenceDate: Date = calendarReferenceDate,
      nextCalendarViewMode: CalendarViewMode = calendarViewMode,
    ) => {
      setIsLoading(true);
      setLoadError(null);

      try {
        const calendarRange = getCalendarRange(nextCalendarReferenceDate, nextCalendarViewMode);
        const effectiveFrom = minDateValue(nextFilters.from, calendarRange.from);
        const effectiveTo = maxDateValue(nextFilters.to, calendarRange.to);
        const [accountsData, categoriesData, entriesData] = await Promise.all([
          getFinancialAccounts(),
          getTransactionCategories(),
          getScheduledEntries({
            status: nextFilters.status || undefined,
            from: effectiveFrom || undefined,
            to: effectiveTo || undefined,
          }),
        ]);

        setFinancialAccounts(accountsData);
        setCategories(categoriesData);
        setScheduledEntries(entriesData);
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
    [calendarReferenceDate, calendarViewMode, logout],
  );

  useEffect(() => {
    if (status !== "authenticated") {
      return;
    }

    const nextFilters = createInitialFilters();
    setFilters(nextFilters);
    void loadPageData(nextFilters);
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

  function updateCalendarView(
    nextReferenceDate: Date,
    nextViewMode: CalendarViewMode = calendarViewMode,
  ) {
    setCalendarReferenceDate(nextReferenceDate);
    setCalendarViewMode(nextViewMode);
    setSelectedCalendarDate(toDateKey(nextReferenceDate));
    void loadPageData(filters, nextReferenceDate, nextViewMode);
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
      await loadPageData(filters);
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
    action: "complete" | "skip" | "cancel",
  ) {
    setSubmitError(null);
    setSubmitSuccess(null);
    setActionEntryId(entry.occurrenceKey);

    try {
      if (action === "complete") {
        await completeScheduledEntry(entry.scheduledEntryId, entry.occurrenceDate);
        setSubmitSuccess("Previsto tratado como realizado com sucesso.");
      }

      if (action === "skip") {
        await skipScheduledEntry(entry.scheduledEntryId, entry.occurrenceDate);
        setSubmitSuccess("Previsto ignorado com sucesso.");
      }

      if (action === "cancel") {
        await cancelScheduledEntry(entry.scheduledEntryId, entry.occurrenceDate);
        setSubmitSuccess("Previsto cancelado com sucesso.");
      }

      await loadPageData(filters);
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

  return (
    <main className={styles.main}>
      <header className={styles.header}>
        <div>
          <p className={styles.eyebrow}>Fase 4</p>
          <h1>Planejamento financeiro</h1>
          <p className={styles.subtitle}>
            Organize compromissos futuros, recorrencias simples e o que ainda
            precisa acontecer sem confundir previsao com transacao real.
          </p>
        </div>

        <div className={styles.headerActions}>
          <div className={styles.userBadge}>
            <span>Usuario autenticado</span>
            <strong>{user?.fullName ?? "Sessao ativa"}</strong>
          </div>
          <button className={styles.secondaryButton} onClick={logout}>
            Sair
          </button>
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
          <span>Agendados ativos</span>
          <strong>{upcomingSummary.scheduledCount}</strong>
          <small>Compromissos futuros sob controle</small>
        </article>
        <article className={styles.summaryCard}>
          <span>Proximos 7 dias</span>
          <strong>{upcomingSummary.nextSevenDaysCount}</strong>
          <small>O que precisa acontecer em breve</small>
        </article>
        <article className={styles.summaryCard}>
          <span>Recorrentes</span>
          <strong>{upcomingSummary.recurringCount}</strong>
          <small>Base viva do planejamento operacional</small>
        </article>
        <article className={styles.summaryCard}>
          <span>Valor previsto</span>
          <strong>{formatCurrency(upcomingSummary.totalScheduledAmount)}</strong>
          <small>Somatorio dos itens agendados filtrados</small>
        </article>
      </section>

      <section className={styles.statusStrip}>
        <div className={styles.statusPill}>
          <span>Agendados</span>
          <strong>{entriesByStatus.scheduled}</strong>
        </div>
        <div className={styles.statusPill}>
          <span>Realizados</span>
          <strong>{entriesByStatus.completed}</strong>
        </div>
        <div className={styles.statusPill}>
          <span>Ignorados</span>
          <strong>{entriesByStatus.skipped}</strong>
        </div>
        <div className={styles.statusPill}>
          <span>Cancelados</span>
          <strong>{entriesByStatus.cancelled}</strong>
        </div>
      </section>

      <section className={styles.forecastGrid}>
        <article className={styles.forecastCard}>
          <div className={styles.forecastCardHeader}>
            <div>
              <span className={styles.horizonEyebrow}>Previsao operacional</span>
              <h2>Saldo previsto do periodo</h2>
            </div>
            <strong className={operationalForecast.netAmount >= 0 ? styles.forecastPositive : styles.forecastNegative}>
              {formatCurrency(operationalForecast.netAmount)}
            </strong>
          </div>
          <p className={styles.forecastText}>
            Diferenca entre receitas e despesas ainda agendadas dentro do recorte atual.
          </p>
          <div className={styles.forecastSplit}>
            <div className={styles.forecastMetric}>
              <span>Receitas previstas</span>
              <strong>{formatCurrency(operationalForecast.incomeAmount)}</strong>
            </div>
            <div className={styles.forecastMetric}>
              <span>Despesas previstas</span>
              <strong>{formatCurrency(operationalForecast.expenseAmount)}</strong>
            </div>
          </div>
        </article>

        <article className={styles.forecastCard}>
          <div className={styles.forecastCardHeader}>
            <div>
              <span className={styles.horizonEyebrow}>Leitura do tratamento</span>
              <h2>Ritmo operacional</h2>
            </div>
            <strong>{operationalForecast.completionRate}%</strong>
          </div>
          <p className={styles.forecastText}>
            Percentual dos itens do recorte atual que ja foram tratados como realizados, ignorados ou cancelados.
          </p>
          <div className={styles.forecastSplit}>
            <div className={styles.forecastMetric}>
              <span>Itens tratados</span>
              <strong>{operationalForecast.treatedCount}</strong>
            </div>
            <div className={styles.forecastMetric}>
              <span>Ainda agendados</span>
              <strong>{entriesByStatus.scheduled}</strong>
            </div>
          </div>
        </article>

        <article className={styles.forecastCard}>
          <div className={styles.forecastCardHeader}>
            <div>
              <span className={styles.horizonEyebrow}>Concentracao futura</span>
              <h2>Mes de maior pressao</h2>
            </div>
            <strong>
              {operationalForecast.peakExpenseMonth
                ? formatCurrency(operationalForecast.peakExpenseMonth.expenseAmount)
                : "Sem leitura"}
            </strong>
          </div>
          <p className={styles.forecastText}>
            {operationalForecast.peakExpenseMonth
              ? `${operationalForecast.peakExpenseMonth.label} concentra o maior volume de despesas agendadas no recorte.`
              : "Ainda nao ha despesas agendadas suficientes para destacar um mes de maior carga."}
          </p>
          <div className={styles.forecastSplit}>
            <div className={styles.forecastMetric}>
              <span>Mes destacado</span>
              <strong>{operationalForecast.peakExpenseMonth?.label ?? "Sem mes"}</strong>
            </div>
            <div className={styles.forecastMetric}>
              <span>Itens no mes</span>
              <strong>{operationalForecast.peakExpenseMonth?.entriesCount ?? 0}</strong>
            </div>
          </div>
        </article>
      </section>

      <section className={styles.forecastBoard}>
        <div className={styles.listHeader}>
          <div>
            <h2>Projecao mensal simples</h2>
            <p>
              Veja como receitas, despesas e saldo previsto se distribuem pelos proximos meses dentro do recorte atual.
            </p>
          </div>
        </div>

        {operationalForecast.monthlyForecast.length > 0 ? (
          <div className={styles.forecastMonthGrid}>
            {operationalForecast.monthlyForecast.slice(0, 6).map((month) => (
              <article className={styles.forecastMonthCard} key={month.month}>
                <div className={styles.forecastMonthHeader}>
                  <div>
                    <h3>{month.label}</h3>
                    <p>{month.entriesCount} ocorrencia(s) agendada(s)</p>
                  </div>
                  <strong className={month.netAmount >= 0 ? styles.forecastPositive : styles.forecastNegative}>
                    {formatCurrency(month.netAmount)}
                  </strong>
                </div>
                <div className={styles.forecastMonthMetrics}>
                  <div className={styles.forecastMetric}>
                    <span>Receitas</span>
                    <strong>{formatCurrency(month.incomeAmount)}</strong>
                  </div>
                  <div className={styles.forecastMetric}>
                    <span>Despesas</span>
                    <strong>{formatCurrency(month.expenseAmount)}</strong>
                  </div>
                </div>
              </article>
            ))}
          </div>
        ) : (
          <div className={styles.focusEmpty}>
            Ainda nao ha ocorrencias agendadas suficientes no recorte para montar uma projecao mensal.
          </div>
        )}
      </section>

      <section className={styles.monthDecisionGrid}>
        <article className={styles.monthDecisionCard}>
          <div className={styles.listHeader}>
            <div>
              <h2>Meses criticos</h2>
              <p>
                Destaques de meses que pedem mais atencao por saldo negativo, carga elevada ou concentracao de ocorrencias.
              </p>
            </div>
          </div>

          {operationalForecast.criticalMonths.length > 0 ? (
            <div className={styles.monthDecisionList}>
              {operationalForecast.criticalMonths.slice(0, 4).map((month) => (
                <article
                  className={`${styles.monthDecisionItem} ${
                    month.netAmount < 0 ? styles.monthDecisionItemAlert : styles.monthDecisionItemWarm
                  }`}
                  key={month.month}
                >
                  <div className={styles.monthDecisionHeader}>
                    <div>
                      <h3>{month.label}</h3>
                      <p>Severidade {month.severityLabel}</p>
                    </div>
                    <strong className={month.netAmount >= 0 ? styles.forecastPositive : styles.forecastNegative}>
                      {formatCurrency(month.netAmount)}
                    </strong>
                  </div>
                  <div className={styles.monthDecisionMetrics}>
                    <span>Carga de despesas: {month.loadShare}%</span>
                    <span>Ocorrencias: {month.entriesCount}</span>
                    <span>Receitas: {formatCurrency(month.incomeAmount)}</span>
                    <span>Despesas: {formatCurrency(month.expenseAmount)}</span>
                  </div>
                </article>
              ))}
            </div>
          ) : (
            <div className={styles.focusEmpty}>
              Nenhum mes do recorte atual apresenta sinal critico relevante.
            </div>
          )}
        </article>

        <article className={styles.monthDecisionCard}>
          <div className={styles.listHeader}>
            <div>
              <h2>Prioridades operacionais</h2>
              <p>
                Ordem simples do que merece decisao agora para manter o planejamento controlado.
              </p>
            </div>
          </div>

          {operationalForecast.operationalPriorities.length > 0 ? (
            <div className={styles.priorityList}>
              {operationalForecast.operationalPriorities.map((priority) => (
                <div className={styles.priorityItem} key={priority}>
                  <strong>Prioridade</strong>
                  <p>{priority}</p>
                </div>
              ))}
            </div>
          ) : (
            <div className={styles.focusEmpty}>
              Nenhuma prioridade operacional adicional foi identificada no recorte atual.
            </div>
          )}
        </article>
      </section>

      <section className={styles.horizonGrid}>
        <article className={styles.horizonCard}>
          <div className={styles.horizonHeader}>
            <div>
              <span className={styles.horizonEyebrow}>Radar imediato</span>
              <h2>O que pede atencao agora</h2>
            </div>
            <strong>{futureFocus.dueToday.length + futureFocus.nextThreeDays.length + futureFocus.overdue.length}</strong>
          </div>

          <div className={styles.horizonBuckets}>
            <div className={styles.horizonBucket}>
              <span>Hoje</span>
              <strong>{futureFocus.dueToday.length}</strong>
              <small>Compromissos que vencem hoje</small>
            </div>
            <div className={styles.horizonBucket}>
              <span>Proximos 3 dias</span>
              <strong>{futureFocus.nextThreeDays.length}</strong>
              <small>Janela curta de decisao</small>
            </div>
            <div className={styles.horizonBucket}>
              <span>Em atraso</span>
              <strong>{futureFocus.overdue.length}</strong>
              <small>Itens que pedem revisao operacional</small>
            </div>
          </div>

          <div className={styles.focusList}>
            {[...futureFocus.overdue, ...futureFocus.dueToday, ...futureFocus.nextThreeDays]
              .slice(0, 5)
              .map((entry) => {
                const diff = daysUntil(entry.occurrenceDate);
                const toneClass = diff !== null && diff < 0
                  ? styles.focusItemAlert
                  : diff === 0
                    ? styles.focusItemToday
                    : styles.focusItemSoon;

                return (
                  <article className={`${styles.focusItem} ${toneClass}`} key={entry.occurrenceKey}>
                    <div>
                      <h3>{entry.description ?? entry.transactionCategoryName}</h3>
                      <p>{entry.financialAccountName} | {TYPE_LABELS[entry.type]} | {formatDate(entry.occurrenceDate)}</p>
                    </div>
                    <strong>{formatCurrency(entry.amount)}</strong>
                  </article>
                );
              })}
            {futureFocus.dueToday.length + futureFocus.nextThreeDays.length + futureFocus.overdue.length === 0 ? (
              <div className={styles.focusEmpty}>
                Nenhum item sensivel no curtissimo prazo dentro dos filtros atuais.
              </div>
            ) : null}
          </div>
        </article>

        <article className={styles.horizonCard}>
          <div className={styles.horizonHeader}>
            <div>
              <span className={styles.horizonEyebrow}>Leitura do futuro</span>
              <h2>Horizonte por mes</h2>
            </div>
            <strong>{monthGroups.length}</strong>
          </div>

          <div className={styles.monthBoard}>
            {monthGroups.slice(0, 4).map((group) => (
              <article className={styles.monthCard} key={group.month}>
                <div className={styles.monthCardHeader}>
                  <div>
                    <h3>{group.label}</h3>
                    <p>{group.entries.length} previsto(s) ativos</p>
                  </div>
                  <strong>{formatCurrency(group.totalAmount)}</strong>
                </div>
                <div className={styles.monthCardList}>
                  {group.entries.slice(0, 3).map((entry) => (
                    <div className={styles.monthCardItem} key={entry.occurrenceKey}>
                      <span>{formatDate(entry.occurrenceDate)}</span>
                      <strong>{entry.description ?? entry.transactionCategoryName}</strong>
                      <small>{entry.financialAccountName}</small>
                    </div>
                  ))}
                </div>
              </article>
            ))}
            {monthGroups.length === 0 ? (
              <div className={styles.focusEmpty}>
                Ainda nao ha recorrencias ou previstos ativos organizados no horizonte mensal.
              </div>
            ) : null}
          </div>
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
        <form
          className={styles.filtersForm}
          onSubmit={async (event) => {
            event.preventDefault();
            await loadPageData(filters);
          }}
        >
          <label className={styles.field}>
            <span>Status</span>
            <select
              value={filters.status}
              onChange={(event) =>
                setFilters((current) => ({
                  ...current,
                  status: event.target.value as FiltersState["status"],
                }))
              }
            >
              <option value="">Todos</option>
              <option value="scheduled">Agendado</option>
              <option value="completed">Realizado</option>
              <option value="skipped">Ignorado</option>
              <option value="cancelled">Cancelado</option>
            </select>
          </label>

          <label className={styles.field}>
            <span>De</span>
            <input
              type="date"
              value={filters.from}
              onChange={(event) =>
                setFilters((current) => ({
                  ...current,
                  from: event.target.value,
                }))
              }
            />
          </label>

          <label className={styles.field}>
            <span>Ate</span>
            <input
              type="date"
              value={filters.to}
              onChange={(event) =>
                setFilters((current) => ({
                  ...current,
                  to: event.target.value,
                }))
              }
            />
          </label>

          <div className={styles.filterActions}>
            <button
              className={styles.secondaryButton}
              type="button"
              onClick={() => {
                const next = createInitialFilters();
                setFilters(next);
                void loadPageData(next);
              }}
            >
              Resetar
            </button>
            <button className={styles.primaryButton} type="submit">
              Aplicar filtros
            </button>
          </div>
        </form>
      </section>

      <section className={styles.calendarCard}>
        <div className={styles.calendarHeader}>
          <div>
            <p className={styles.calendarEyebrow}>Calendario simples</p>
            <h2>{calendarTitle}</h2>
            <p>
              Distribua os previstos no tempo com uma visao semanal ou mensal,
              mantendo o mesmo backend e a mesma base operacional do modulo.
            </p>
          </div>

          <div className={styles.calendarToolbar}>
            <div className={styles.calendarModeSwitch}>
              <button
                className={calendarViewMode === "week" ? styles.calendarModeActive : styles.secondaryButton}
                type="button"
                onClick={() => updateCalendarView(calendarReferenceDate, "week")}
              >
                Semana
              </button>
              <button
                className={calendarViewMode === "month" ? styles.calendarModeActive : styles.secondaryButton}
                type="button"
                onClick={() => updateCalendarView(calendarReferenceDate, "month")}
              >
                Mes
              </button>
            </div>

            <div className={styles.calendarNav}>
              <button
                className={styles.secondaryButton}
                type="button"
                onClick={() => {
                  const nextDate = calendarViewMode === "week"
                    ? addDateWeeks(calendarReferenceDate, -1)
                    : addDateMonths(calendarReferenceDate, -1);
                  updateCalendarView(nextDate);
                }}
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
                onClick={() => {
                  const nextDate = calendarViewMode === "week"
                    ? addDateWeeks(calendarReferenceDate, 1)
                    : addDateMonths(calendarReferenceDate, 1);
                  updateCalendarView(nextDate);
                }}
              >
                Proximo
              </button>
            </div>
          </div>
        </div>

        <div className={styles.calendarWeekdayRow}>
          {["Seg", "Ter", "Qua", "Qui", "Sex", "Sab", "Dom"].map((weekday) => (
            <span key={weekday}>{weekday}</span>
          ))}
        </div>

        <div className={`${styles.calendarGrid} ${calendarViewMode === "week" ? styles.calendarGridWeek : styles.calendarGridMonth}`}>
          {visibleCalendarDays.map((day) => (
            <button
              type="button"
              className={`${styles.calendarDay} ${day.isToday ? styles.calendarDayToday : ""} ${!day.isCurrentMonth ? styles.calendarDayMuted : ""} ${selectedCalendarDate === day.key ? styles.calendarDaySelected : ""}`}
              key={day.key}
              onClick={() => setSelectedCalendarDate(day.key)}
            >
              <div className={styles.calendarDayHeader}>
                <strong>{day.date.getDate().toString().padStart(2, "0")}</strong>
                <span>{day.entries.length ? `${day.entries.length} item(ns)` : "Livre"}</span>
              </div>

              <div className={styles.calendarDayList}>
                {day.entries.slice(0, 3).map((entry) => (
                  <div className={styles.calendarEntry} key={entry.occurrenceKey}>
                    <div className={styles.calendarEntryMeta}>
                      <span>{TYPE_LABELS[entry.type]}</span>
                      <small className={`${styles.calendarEntryStatus} ${styles[`calendarEntryStatus${entry.status}`]}`}>
                        {STATUS_LABELS[entry.status]}
                      </small>
                    </div>
                    <strong>{entry.description ?? entry.transactionCategoryName}</strong>
                    <small>{formatCurrency(entry.amount)}</small>
                  </div>
                ))}
                {day.entries.length > 3 ? (
                  <div className={styles.calendarOverflow}>
                    +{day.entries.length - 3} previsto(s) neste dia
                  </div>
                ) : null}
              </div>
            </button>
          ))}
        </div>

        <aside className={styles.calendarDrawer}>
          <div className={styles.calendarDrawerHeader}>
            <div>
              <span className={styles.calendarEyebrow}>Dia selecionado</span>
              <h3>{selectedCalendarDateLabel}</h3>
              <p>
                {selectedCalendarEntries.length
                  ? "Veja os previstos do dia e aja sem sair do calendario."
                  : "Nenhum previsto neste dia. Use o atalho para criar um novo compromisso com a data predefinida."}
              </p>
            </div>
            <button
              className={styles.primaryButton}
              type="button"
              onClick={() => openModalForDate(selectedCalendarDate)}
              disabled={!hasOperationalBase}
            >
              Novo neste dia
            </button>
          </div>

          <div className={styles.calendarDrawerSummary}>
            <div className={styles.calendarDrawerMetric}>
              <span>Total previsto</span>
              <strong>{formatCurrency(selectedCalendarSummary.totalAmount)}</strong>
            </div>
            <div className={styles.calendarDrawerMetric}>
              <span>Receitas</span>
              <strong>{formatCurrency(selectedCalendarSummary.incomeAmount)}</strong>
            </div>
            <div className={styles.calendarDrawerMetric}>
              <span>Despesas</span>
              <strong>{formatCurrency(selectedCalendarSummary.expenseAmount)}</strong>
            </div>
          </div>

          {selectedCalendarEntries.length > 0 ? (
            <div className={styles.calendarDrawerList}>
              {selectedCalendarEntries.map((entry) => (
                <article className={styles.calendarDrawerItem} key={entry.occurrenceKey}>
                  <div className={styles.calendarDrawerItemTop}>
                    <div>
                      <h4>{entry.description ?? entry.transactionCategoryName}</h4>
                      <p>{entry.financialAccountName} | {entry.transactionCategoryName}</p>
                    </div>
                    <strong>{formatCurrency(entry.amount)}</strong>
                  </div>
                  <div className={styles.calendarDrawerItemMeta}>
                    <span>{TYPE_LABELS[entry.type]}</span>
                    <span>{MODE_LABELS[entry.planningMode]}{entry.recurrenceFrequency ? ` | ${FREQUENCY_LABELS[entry.recurrenceFrequency]}` : ""}</span>
                    <span className={`${styles.calendarEntryStatus} ${styles[`calendarEntryStatus${entry.status}`]}`}>
                      {STATUS_LABELS[entry.status]}
                    </span>
                  </div>
                  <div className={styles.calendarDrawerActions}>
                    <button
                      className={styles.secondaryButton}
                      type="button"
                      onClick={() => openEditModal(entry)}
                      disabled={!entry.canEdit || actionEntryId === entry.occurrenceKey}
                    >
                      Editar
                    </button>
                    <button
                      className={styles.secondaryButton}
                      type="button"
                      onClick={() => void handleEntryAction(entry, "complete")}
                      disabled={!entry.canAct || actionEntryId === entry.occurrenceKey}
                    >
                      {actionEntryId === entry.occurrenceKey ? "Salvando..." : "Realizar"}
                    </button>
                    <button
                      className={styles.secondaryButton}
                      type="button"
                      onClick={() => void handleEntryAction(entry, "skip")}
                      disabled={!entry.canAct || actionEntryId === entry.occurrenceKey}
                    >
                      {actionEntryId === entry.occurrenceKey ? "Salvando..." : "Ignorar"}
                    </button>
                    <button
                      className={styles.secondaryButton}
                      type="button"
                      onClick={() => void handleEntryAction(entry, "cancel")}
                      disabled={!entry.canAct || actionEntryId === entry.occurrenceKey}
                    >
                      {actionEntryId === entry.occurrenceKey ? "Salvando..." : "Cancelar"}
                    </button>
                  </div>
                </article>
              ))}
            </div>
          ) : (
            <div className={styles.calendarDrawerEmpty}>
              Este dia esta livre dentro dos filtros atuais.
            </div>
          )}
        </aside>
      </section>

      <section className={styles.listCard}>
        <div className={styles.listHeader}>
          <div>
            <h2>Seus previstos</h2>
            <p>
              Veja o que esta por vir, trate cada compromisso e mantenha o
              planejamento do futuro imediato explicavel.
            </p>
          </div>
          <button
            className={styles.secondaryButton}
            onClick={() => void loadPageData(filters)}
          >
            Recarregar lista
          </button>
        </div>

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
                onClick={() => void loadPageData(filters)}
              >
                Tentar novamente
              </button>
            }
          />
        ) : null}

        {status !== "loading" && !isLoading && !loadError && filteredEntries.length === 0 ? (
          <SharedState
            eyebrow="Planejamento"
            title="Nenhum previsto encontrado"
            description="Crie seu primeiro lancamento planejado para abrir a camada de planejamento da Fase 4 com controle simples e auditavel."
            tone="empty"
            compact
            actions={
              <button
                className={styles.primaryButton}
                onClick={openModal}
                disabled={!hasOperationalBase}
              >
                Novo previsto
              </button>
            }
          />
        ) : null}

        {status !== "loading" && !isLoading && !loadError && filteredEntries.length > 0 ? (
          <div className={styles.entryList}>
            {filteredEntries.map((entry) => {
              const untilNext = daysUntil(entry.occurrenceDate);
              const canAct = entry.canAct && isScheduledStatus(entry.status);

              return (
                <article className={styles.entryCard} key={entry.occurrenceKey}>
                  <div className={styles.entryTopRow}>
                    <div>
                      <h3>{entry.description ?? entry.transactionCategoryName}</h3>
                      <p>
                        {entry.financialAccountName} | {entry.transactionCategoryName}
                      </p>
                      <span className={styles.mutedText}>
                        {TYPE_LABELS[entry.type]} | {MODE_LABELS[entry.planningMode]}
                        {entry.recurrenceFrequency
                          ? ` | ${FREQUENCY_LABELS[entry.recurrenceFrequency]}`
                          : ""}
                      </span>
                    </div>

                    <span
                      className={`${styles.statusBadge} ${styles[`statusBadge${entry.status}`]}`}
                    >
                      {STATUS_LABELS[entry.status]}
                    </span>
                  </div>

                  <div className={styles.entryMetaRow}>
                    <div>
                      <span>Valor previsto</span>
                      <strong>{formatCurrency(entry.amount)}</strong>
                    </div>
                    <div>
                      <span>Inicio</span>
                      <strong>{formatDate(entry.startDate)}</strong>
                    </div>
                    <div>
                      <span>Competencia</span>
                      <strong>{formatDate(entry.occurrenceDate)}</strong>
                    </div>
                    <div>
                      <span>Janela operacional</span>
                      <strong>
                        {!isScheduledStatus(entry.status)
                          ? "Tratado"
                          : untilNext === null
                          ? "Sem ocorrencia ativa"
                          : untilNext < 0
                            ? `${Math.abs(untilNext)} dia(s) em atraso`
                            : untilNext === 0
                              ? "Hoje"
                              : `${untilNext} dia(s)`}
                      </strong>
                    </div>
                  </div>

                  <div className={styles.entryFooter}>
                    <div className={styles.entryHistory}>
                      <span>Tratado em</span>
                      <strong>{formatDateTime(entry.treatedAtUtc)}</strong>
                      <small>
                        {entry.endDate
                          ? `Recorrencia vai ate ${formatDate(entry.endDate)}`
                          : "Sem data final definida"}
                      </small>
                    </div>

                    <div className={styles.actionBar}>
                      <button
                        className={styles.secondaryButton}
                        onClick={() => openEditModal(entry)}
                        disabled={!entry.canEdit || actionEntryId === entry.occurrenceKey}
                      >
                        Editar
                      </button>
                      <button
                        className={styles.secondaryButton}
                        onClick={() => void handleEntryAction(entry, "complete")}
                        disabled={!canAct || actionEntryId === entry.occurrenceKey}
                      >
                        {actionEntryId === entry.occurrenceKey ? "Salvando..." : "Marcar como realizado"}
                      </button>
                      <button
                        className={styles.secondaryButton}
                        onClick={() => void handleEntryAction(entry, "skip")}
                        disabled={!canAct || actionEntryId === entry.occurrenceKey}
                      >
                        {actionEntryId === entry.occurrenceKey ? "Salvando..." : "Ignorar"}
                      </button>
                      <button
                        className={styles.secondaryButton}
                        onClick={() => void handleEntryAction(entry, "cancel")}
                        disabled={!canAct || actionEntryId === entry.occurrenceKey}
                      >
                        {actionEntryId === entry.occurrenceKey ? "Salvando..." : "Cancelar"}
                      </button>
                    </div>
                  </div>
                </article>
              );
            })}
          </div>
        ) : null}
      </section>

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

"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useAuth } from "@/features/auth/AuthProvider";
import { ApiError } from "@/services/api-client";
import { getCreditCards } from "@/services/credit-cards-service";
import { getFinancialAccounts } from "@/services/financial-accounts-service";
import { adjustInvoice, closeInvoice, createInvoice, getCardExpenses, getInvoices, payInvoice, registerCardExpense } from "@/services/invoices-service";
import { getTransactionCategories } from "@/services/transaction-categories-service";
import type { CreditCard } from "@/types/credit-cards";
import type { FinancialAccount } from "@/types/financial-accounts";
import type { AdjustInvoiceInput, CreateInvoiceInput, CreditCardExpense, Invoice, InvoiceAdjustmentType, RegisterCardExpenseInput } from "@/types/invoices";
import type { TransactionCategory } from "@/types/transaction-categories";
import styles from "./InvoicesPage.module.css";

function buildInitialMonthValue() {
  const now = new Date();
  const year = now.getFullYear();
  const month = `${now.getMonth() + 1}`.padStart(2, "0");
  return `${year}-${month}`;
}

function buildTodayValue() {
  const now = new Date();
  const year = now.getFullYear();
  const month = `${now.getMonth() + 1}`.padStart(2, "0");
  const day = `${now.getDate()}`.padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function createExpenseForm(creditCardId = ""): RegisterCardExpenseInput {
  return {
    creditCardId,
    transactionCategoryId: "",
    amount: 0,
    occurredOn: buildTodayValue(),
    description: "",
    targetInvoiceId: undefined,
    installmentCount: 1,
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
    return "Nao pago";
  }

  const iso = value.includes("T") ? value : `${value}T00:00:00`;
  return new Intl.DateTimeFormat("pt-BR").format(new Date(iso));
}

function formatReferenceMonth(year: number, month: number) {
  return `${month.toString().padStart(2, "0")}/${year}`;
}

function resolveReferenceMonth(occurredOn: string, closingDay: number) {
  const [yearRaw, monthRaw, dayRaw] = occurredOn.split("-");
  const year = Number(yearRaw);
  const month = Number(monthRaw);
  const day = Number(dayRaw);

  if (!year || !month || !day) {
    return null;
  }

  const closingDate = new Date(year, month - 1, closingDay);
  const purchaseDate = new Date(year, month - 1, day);

  if (purchaseDate.getTime() <= closingDate.getTime()) {
    return { year, month };
  }

  const nextReference = new Date(year, month, 1);
  return { year: nextReference.getFullYear(), month: nextReference.getMonth() + 1 };
}


const adjustmentTypeOptions: Array<{ value: InvoiceAdjustmentType; label: string; hint: string }> = [
  { value: "credit", label: "Credito", hint: "Remove valor da fatura quando houver credito a favor do cliente." },
  { value: "discount", label: "Desconto", hint: "Aplica reducao comercial ou ajuste de abatimento." },
  { value: "fee", label: "Encargo", hint: "Acrescenta tarifa ou custo operacional na fatura." },
  { value: "interest", label: "Juros", hint: "Acrescenta juros financeiros na fatura." },
  { value: "penalty", label: "Multa", hint: "Acrescenta multa, geralmente por atraso." },
  { value: "manualIncrease", label: "Correcao manual para mais", hint: "Aumenta manualmente o valor para refletir a realidade do emissor." },
  { value: "manualDecrease", label: "Correcao manual para menos", hint: "Reduz manualmente o valor para refletir a realidade do emissor." },
];

function formatAdjustmentTypeLabel(value: InvoiceAdjustmentType) {
  return adjustmentTypeOptions.find((option) => option.value === value)?.label ?? value;
}
function formatInstallmentLabel(expense: CreditCardExpense) {
  return expense.installmentCount > 1
    ? `Parcela ${expense.installmentNumber}/${expense.installmentCount}`
    : "Compra a vista";
}

export function InvoicesPage() {
  const { logout, status, user } = useAuth();
  const [focusedInvoiceId, setFocusedInvoiceId] = useState<string>("");
  const [creditCards, setCreditCards] = useState<CreditCard[]>([]);
  const [financialAccounts, setFinancialAccounts] = useState<FinancialAccount[]>([]);
  const [categories, setCategories] = useState<TransactionCategory[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [expenses, setExpenses] = useState<CreditCardExpense[]>([]);
  const [selectedCreditCardId, setSelectedCreditCardId] = useState<string>("");
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isPaymentModalOpen, setIsPaymentModalOpen] = useState(false);
  const [isExpenseModalOpen, setIsExpenseModalOpen] = useState(false);
  const [isAdjustmentModalOpen, setIsAdjustmentModalOpen] = useState(false);
  const [closingInvoiceId, setClosingInvoiceId] = useState<string>("");
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null);
  const [adjustmentInvoice, setAdjustmentInvoice] = useState<Invoice | null>(null);
  const [monthValue, setMonthValue] = useState(buildInitialMonthValue());
  const [creditCardId, setCreditCardId] = useState<string>("");
  const [paymentAccountId, setPaymentAccountId] = useState<string>("");
  const [paymentAmount, setPaymentAmount] = useState<number>(0);
  const [adjustmentType, setAdjustmentType] = useState<InvoiceAdjustmentType>("credit");
  const [adjustmentAmount, setAdjustmentAmount] = useState<number>(0);
  const [expenseForm, setExpenseForm] = useState<RegisterCardExpenseInput>(createExpenseForm());
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitSuccess, setSubmitSuccess] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const expenseCategories = useMemo(() => categories.filter((category) => category.type === "expense"), [categories]);
  const expensesByInvoice = useMemo(() => {
    const map = new Map<string, CreditCardExpense[]>();
    for (const expense of expenses) {
      const current = map.get(expense.invoiceId) ?? [];
      current.push(expense);
      map.set(expense.invoiceId, current);
    }
    return map;
  }, [expenses]);

  const selectedAdjustmentTypeOption = useMemo(() => adjustmentTypeOptions.find((option) => option.value === adjustmentType) ?? adjustmentTypeOptions[0], [adjustmentType]);

  const expenseDestinationContext = useMemo(() => {
    const selectedCard = creditCards.find((card) => card.id === expenseForm.creditCardId);
    if (!selectedCard || !expenseForm.occurredOn) {
      return null;
    }

    const reference = resolveReferenceMonth(expenseForm.occurredOn, selectedCard.closingDay);
    if (!reference) {
      return null;
    }

    const naturalInvoice = invoices.find((invoice) =>
      invoice.creditCardId === selectedCard.id
      && invoice.referenceYear === reference.year
      && invoice.referenceMonth === reference.month);

    if (!naturalInvoice || naturalInvoice.status === "open") {
      return null;
    }

    const nextOpenInvoice = invoices
      .filter((invoice) => invoice.creditCardId === selectedCard.id && invoice.status === "open")
      .filter((invoice) => invoice.referenceYear > reference.year || (invoice.referenceYear === reference.year && invoice.referenceMonth > reference.month))
      .sort((left, right) => {
        if (left.referenceYear !== right.referenceYear) {
          return left.referenceYear - right.referenceYear;
        }

        return left.referenceMonth - right.referenceMonth;
      })[0];

    const nextReferenceDate = new Date(reference.year, reference.month, 1);
    const nextReferenceLabel = nextOpenInvoice
      ? formatReferenceMonth(nextOpenInvoice.referenceYear, nextOpenInvoice.referenceMonth)
      : formatReferenceMonth(nextReferenceDate.getFullYear(), nextReferenceDate.getMonth() + 1);

    return {
      naturalInvoice,
      naturalReferenceLabel: formatReferenceMonth(naturalInvoice.referenceYear, naturalInvoice.referenceMonth),
      nextReferenceLabel,
    };
  }, [creditCards, expenseForm.creditCardId, expenseForm.occurredOn, invoices]);
  const loadData = useCallback(async (creditCardFilter?: string) => {
    setIsLoading(true);
    setLoadError(null);

    try {
      const [cards, accounts, transactionCategories, invoiceList, expenseList] = await Promise.all([
        getCreditCards(),
        getFinancialAccounts(),
        getTransactionCategories(),
        getInvoices(creditCardFilter || undefined),
        getCardExpenses({ creditCardId: creditCardFilter || undefined }),
      ]);
      setCreditCards(cards);
      setFinancialAccounts(accounts);
      setCategories(transactionCategories);
      setInvoices(invoiceList);
      setExpenses(expenseList);
      if (!creditCardId && cards.length > 0) {
        setCreditCardId(cards[0].id);
      }
      if (!paymentAccountId && accounts.length > 0) {
        setPaymentAccountId(accounts[0].id);
      }
      if (!expenseForm.creditCardId && cards.length > 0) {
        setExpenseForm((current) => ({ ...current, creditCardId: cards[0].id }));
      }
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      const message = error instanceof ApiError ? error.message : "Nao foi possivel carregar suas faturas agora.";
      setLoadError(message);
    } finally {
      setIsLoading(false);
    }
  }, [creditCardId, expenseForm.creditCardId, logout, paymentAccountId]);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const nextInvoiceId = params.get("invoiceId") ?? "";
    const nextCreditCardId = params.get("creditCardId") ?? "";

    setFocusedInvoiceId(nextInvoiceId);
    if (nextCreditCardId) {
      setSelectedCreditCardId((current) => (current === nextCreditCardId ? current : nextCreditCardId));
    }
  }, []);

  useEffect(() => {
    if (status !== "authenticated") {
      return;
    }

    void loadData(selectedCreditCardId || undefined);
  }, [status, selectedCreditCardId, loadData]);

  useEffect(() => {
    if (!focusedInvoiceId || isLoading || invoices.length === 0) {
      return;
    }

    const element = document.getElementById(`invoice-${focusedInvoiceId}`);
    if (!element) {
      return;
    }

    requestAnimationFrame(() => {
      element.scrollIntoView({ behavior: "smooth", block: "center" });
    });
  }, [focusedInvoiceId, isLoading, invoices]);

  function mergeInvoice(nextInvoice: Invoice) {
    setInvoices((current) => {
      const next = current.some((invoice) => invoice.id === nextInvoice.id)
        ? current.map((invoice) => (invoice.id === nextInvoice.id ? nextInvoice : invoice))
        : [nextInvoice, ...current];

      return next.sort((left, right) => {
        if (left.referenceYear !== right.referenceYear) {
          return right.referenceYear - left.referenceYear;
        }

        if (left.referenceMonth !== right.referenceMonth) {
          return right.referenceMonth - left.referenceMonth;
        }

        return new Date(right.createdAtUtc).getTime() - new Date(left.createdAtUtc).getTime();
      });
    });
  }

  function handleOpenCreateModal() {
    setMonthValue(buildInitialMonthValue());
    setCreditCardId(creditCards[0]?.id ?? "");
    setSubmitError(null);
    setSubmitSuccess(null);
    setIsCreateModalOpen(true);
  }

  function handleCloseCreateModal() {
    if (isSubmitting) {
      return;
    }

    setIsCreateModalOpen(false);
    setSubmitError(null);
  }

  function handleOpenExpenseModal() {
    setExpenseForm(createExpenseForm(creditCards[0]?.id ?? ""));
    setSubmitError(null);
    setSubmitSuccess(null);
    setIsExpenseModalOpen(true);
  }

  function handleCloseExpenseModal() {
    if (isSubmitting) {
      return;
    }

    setIsExpenseModalOpen(false);
    setSubmitError(null);
  }


  function handleOpenAdjustmentModal(invoice: Invoice) {
    setAdjustmentInvoice(invoice);
    setAdjustmentType("credit");
    setAdjustmentAmount(0);
    setSubmitError(null);
    setSubmitSuccess(null);
    setIsAdjustmentModalOpen(true);
  }

  function handleCloseAdjustmentModal() {
    if (isSubmitting) {
      return;
    }

    setIsAdjustmentModalOpen(false);
    setAdjustmentInvoice(null);
    setAdjustmentAmount(0);
    setSubmitError(null);
  }
  function handleOpenPaymentModal(invoice: Invoice) {
    setSelectedInvoice(invoice);
    setPaymentAccountId(financialAccounts[0]?.id ?? "");
    setPaymentAmount(invoice.suggestedMinimumPaymentAmount > 0 ? invoice.suggestedMinimumPaymentAmount : invoice.remainingAmount);
    setSubmitError(null);
    setSubmitSuccess(null);
    setIsPaymentModalOpen(true);
  }

  function handleClosePaymentModal() {
    if (isSubmitting) {
      return;
    }

    setIsPaymentModalOpen(false);
    setSelectedInvoice(null);
    setPaymentAmount(0);
    setSubmitError(null);
  }

  async function handleCreateSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitError(null);
    setSubmitSuccess(null);

    if (!creditCardId) {
      setSubmitError("Selecione um cartao para abrir a fatura.");
      return;
    }

    if (!monthValue) {
      setSubmitError("Informe um mes de referencia valido.");
      return;
    }

    const [yearRaw, monthRaw] = monthValue.split("-");
    const payload: CreateInvoiceInput = {
      creditCardId,
      referenceYear: Number(yearRaw),
      referenceMonth: Number(monthRaw),
    };

    setIsSubmitting(true);

    try {
      const created = await createInvoice(payload);
      const matchesFilter = !selectedCreditCardId || created.creditCardId === selectedCreditCardId;
      if (matchesFilter) {
        mergeInvoice(created);
      }
      setSubmitSuccess("Fatura aberta com sucesso.");
      setIsCreateModalOpen(false);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      const message = error instanceof ApiError ? error.message : "Nao foi possivel abrir a fatura agora.";
      setSubmitError(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleExpenseSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitError(null);
    setSubmitSuccess(null);

    if (!expenseForm.creditCardId) {
      setSubmitError("Selecione um cartao para registrar a compra.");
      return;
    }

    if (!expenseForm.transactionCategoryId) {
      setSubmitError("Selecione uma categoria de despesa para a compra.");
      return;
    }

    if (expenseForm.amount <= 0) {
      setSubmitError("Informe um valor maior que zero para a compra.");
      return;
    }

    if (!expenseForm.occurredOn) {
      setSubmitError("Informe a data da compra.");
      return;
    }

    if (expenseForm.installmentCount < 1 || expenseForm.installmentCount > 12) {
      setSubmitError("Informe uma quantidade de parcelas entre 1 e 12.");
      return;
    }

    setIsSubmitting(true);

    try {
      const invoice = await registerCardExpense({
        ...expenseForm,
        targetInvoiceId: expenseForm.targetInvoiceId || undefined,
        description: expenseForm.description?.trim() || undefined,
      });
      const matchesFilter = !selectedCreditCardId || invoice.creditCardId === selectedCreditCardId;
      if (matchesFilter) {
        mergeInvoice(invoice);
      }
      await loadData(selectedCreditCardId || undefined);
      const purchaseModeLabel = expenseForm.installmentCount > 1
        ? `${expenseForm.installmentCount} parcelas distribuidas a partir da fatura ${formatReferenceMonth(invoice.referenceYear, invoice.referenceMonth)}`
        : `fatura ${formatReferenceMonth(invoice.referenceYear, invoice.referenceMonth)}`;
      setSubmitSuccess(`Compra registrada e vinculada a ${purchaseModeLabel}.`);
      setIsExpenseModalOpen(false);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      const message = error instanceof ApiError ? error.message : "Nao foi possivel registrar a compra do cartao agora.";
      setSubmitError(message);
    } finally {
      setIsSubmitting(false);
    }
  }


  async function handleAdjustmentSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitError(null);
    setSubmitSuccess(null);

    if (!adjustmentInvoice) {
      setSubmitError("Selecione uma fatura valida para ajuste.");
      return;
    }

    if (adjustmentAmount <= 0) {
      setSubmitError("Informe um valor maior que zero para o ajuste.");
      return;
    }

    const payload: AdjustInvoiceInput = {
      adjustmentType,
      amount: adjustmentAmount,
    };

    setIsSubmitting(true);

    try {
      const adjusted = await adjustInvoice(adjustmentInvoice.id, payload);
      mergeInvoice(adjusted);
      setSubmitSuccess(`Ajuste ${formatAdjustmentTypeLabel(adjustmentType)} aplicado com sucesso.`);
      setIsAdjustmentModalOpen(false);
      setAdjustmentInvoice(null);
      setAdjustmentAmount(0);
      await loadData(selectedCreditCardId || undefined);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      const message = error instanceof ApiError ? error.message : "Nao foi possivel aplicar o ajuste da fatura agora.";
      setSubmitError(message);
    } finally {
      setIsSubmitting(false);
    }
  }
  async function handleCloseInvoice(invoice: Invoice) {
    setSubmitError(null);
    setSubmitSuccess(null);
    setClosingInvoiceId(invoice.id);

    try {
      const closed = await closeInvoice(invoice.id);
      mergeInvoice(closed);
      setSubmitSuccess(`Fatura ${formatReferenceMonth(closed.referenceYear, closed.referenceMonth)} fechada com sucesso.`);
      await loadData(selectedCreditCardId || undefined);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      const message = error instanceof ApiError ? error.message : "Nao foi possivel fechar a fatura agora.";
      setSubmitError(message);
    } finally {
      setClosingInvoiceId("");
    }
  }

  async function handlePaymentSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitError(null);
    setSubmitSuccess(null);

    if (!selectedInvoice) {
      setSubmitError("Selecione uma fatura valida para pagamento.");
      return;
    }

    if (!paymentAccountId) {
      setSubmitError("Selecione a conta financeira de pagamento.");
      return;
    }

    if (paymentAmount <= 0) {
      setSubmitError("Informe um valor de pagamento maior que zero.");
      return;
    }

    if (paymentAmount > selectedInvoice.remainingAmount) {
      setSubmitError("O valor informado nao pode ser maior que o saldo remanescente da fatura.");
      return;
    }

    setIsSubmitting(true);

    try {
      const paid = await payInvoice(selectedInvoice.id, { financialAccountId: paymentAccountId, amount: paymentAmount });
      mergeInvoice(paid);
      setSubmitSuccess(paid.remainingAmount === 0 ? "Fatura paga com sucesso." : "Pagamento parcial registrado com sucesso.");
      setIsPaymentModalOpen(false);
      setSelectedInvoice(null);
      setPaymentAmount(0);
      await loadData(selectedCreditCardId || undefined);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      const message = error instanceof ApiError ? error.message : "Nao foi possivel pagar a fatura agora.";
      setSubmitError(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  const openInvoices = invoices.filter((invoice) => invoice.status === "open").length;
  const paidInvoices = invoices.filter((invoice) => invoice.status === "paid").length;

  return (
    <main className={styles.main}>
      <header className={styles.header}>
        <div>
          <p className={styles.eyebrow}>Fase 3</p>
          <h1>Faturas</h1>
          <p className={styles.subtitle}>
            Agora o ciclo pode ser fechado explicitamente, travando a fatura atual e consolidando quando novas compras devem seguir para a proxima.
          </p>
        </div>

        <div className={styles.headerActions}>
          <div className={styles.userBadge}>
            <span>Usuario autenticado</span>
            <strong>{user?.fullName ?? "Sessao ativa"}</strong>
          </div>
          <button className={styles.secondaryButton} onClick={logout}>Sair</button>
          <button className={styles.secondaryButton} onClick={handleOpenExpenseModal} disabled={creditCards.length === 0 || expenseCategories.length === 0}>Nova compra</button>
          <button className={styles.primaryButton} onClick={handleOpenCreateModal} disabled={creditCards.length === 0}>Nova fatura</button>
        </div>
      </header>

      <section className={styles.summaryGrid}>
        <article className={styles.summaryCard}>
          <span>Total de faturas</span>
          <strong>{invoices.length}</strong>
          <small>Leitura minima do dominio de credito</small>
        </article>
        <article className={styles.summaryCard}>
          <span>Faturas abertas</span>
          <strong>{openInvoices}</strong>
          <small>Alimentadas por compras reais do cartao</small>
        </article>
        <article className={styles.summaryCard}>
          <span>Faturas pagas</span>
          <strong>{paidInvoices}</strong>
          <small>Coerentes com contas financeiras existentes</small>
        </article>
      </section>

      {submitSuccess ? <div className={styles.feedbackSuccess}>{submitSuccess}</div> : null}

      {creditCards.length > 0 && expenseCategories.length === 0 ? (
        <section className={styles.warningBlock}>
          <strong>Compras no cartao exigem categorias de despesa.</strong>
          <p>Cadastre ao menos uma categoria do tipo despesa para transformar compras reais em valor de fatura.</p>
        </section>
      ) : null}

      <section className={styles.listCard}>
        <div className={styles.listHeader}>
          <div>
            <h2>Suas faturas</h2>
            <p>Consulte por cartao, abra ciclos manualmente quando precisar, acompanhe os totais e inspecione cada compra ligada a fatura.</p>
          </div>
          <div className={styles.listActions}>
            <select className={styles.filterSelect} value={selectedCreditCardId} onChange={(event) => setSelectedCreditCardId(event.target.value)}>
              <option value="">Todos os cartoes</option>
              {creditCards.map((card) => (
                <option key={card.id} value={card.id}>{card.name}</option>
              ))}
            </select>
            <button className={styles.secondaryButton} onClick={() => void loadData(selectedCreditCardId || undefined)}>Recarregar lista</button>
          </div>
        </div>

        {status === "loading" || isLoading ? <div className={styles.skeletonList}><div className={styles.skeletonRow} /><div className={styles.skeletonRow} /><div className={styles.skeletonRow} /></div> : null}

        {status !== "loading" && !isLoading && loadError ? (
          <div className={styles.stateBlock}>
            <h3>Nao foi possivel carregar suas faturas.</h3>
            <p>{loadError}</p>
            <button className={styles.secondaryButton} onClick={() => void loadData(selectedCreditCardId || undefined)}>Tentar novamente</button>
          </div>
        ) : null}

        {status !== "loading" && !isLoading && !loadError && creditCards.length === 0 ? (
          <div className={styles.stateBlock}>
            <h3>Cadastre um cartao antes de abrir faturas</h3>
            <p>O modulo de faturas depende de pelo menos um cartao de credito existente.</p>
          </div>
        ) : null}

        {status !== "loading" && !isLoading && !loadError && financialAccounts.length === 0 ? (
          <div className={styles.stateBlock}>
            <h3>Cadastre uma conta para pagar faturas</h3>
            <p>O pagamento de fatura usa uma conta financeira existente como origem do debito.</p>
          </div>
        ) : null}

        {status !== "loading" && !isLoading && !loadError && creditCards.length > 0 && invoices.length === 0 ? (
          <div className={styles.stateBlock}>
            <h3>Nenhuma fatura aberta ainda</h3>
            <p>Registre uma compra real no cartao para abrir a fatura do ciclo correto automaticamente ou abra o ciclo manualmente quando precisar.</p>
            <div className={styles.actionRow}>
              <button className={styles.secondaryButton} onClick={handleOpenExpenseModal} disabled={expenseCategories.length === 0}>Nova compra</button>
              <button className={styles.primaryButton} onClick={handleOpenCreateModal}>Nova fatura</button>
            </div>
          </div>
        ) : null}

        {status !== "loading" && !isLoading && !loadError && invoices.length > 0 ? (
          <div className={styles.invoiceList}>
            {invoices.map((invoice) => {
              const invoiceExpenses = expensesByInvoice.get(invoice.id) ?? [];

              return (
                <article
                  className={`${styles.invoiceCard} ${invoice.id === focusedInvoiceId ? styles.invoiceCardHighlight : ""}`}
                  id={`invoice-${invoice.id}`}
                  key={invoice.id}
                >
                  <div className={styles.invoiceTopRow}>
                    <div>
                      <h3>{invoice.creditCardName}</h3>
                      <p>
                        Referencia {formatReferenceMonth(invoice.referenceYear, invoice.referenceMonth)}
                        {invoice.creditCardBrand ? ` · ${invoice.creditCardBrand}` : ""}
                      </p>
                    </div>
                    <span className={styles.statusBadge}>{invoice.status}</span>
                  </div>

                  <div className={styles.invoiceMetaRow}>
                    <div><span>Periodo</span><strong>{formatDate(invoice.periodStart)} - {formatDate(invoice.periodEnd)}</strong></div>
                    <div><span>Fechamento</span><strong>{formatDate(invoice.closingDate)}</strong></div>
                    <div><span>Vencimento</span><strong>{formatDate(invoice.dueDate)}</strong></div>
                  </div>

                  <div className={styles.invoiceTotalsGrid}>
                    <div><span>Total atual</span><strong>{formatCurrency(invoice.totalAmount)}</strong></div>
                    <div><span>Ja pago</span><strong>{formatCurrency(invoice.paidAmount)}</strong></div>
                    <div><span>Saldo remanescente</span><strong>{formatCurrency(invoice.remainingAmount)}</strong></div>
                    <div><span>Minimo sugerido</span><strong>{formatCurrency(invoice.suggestedMinimumPaymentAmount)}</strong></div>
                  </div>

                  {(invoice.lateFeeAppliedAmount > 0 || invoice.lateInterestAppliedAmount > 0 || invoice.revolvingInterestAppliedAmount > 0) ? (
                    <div className={styles.chargeSummary}>
                      <span>Encargos aplicados</span>
                      <small>Multa: {formatCurrency(invoice.lateFeeAppliedAmount)} · Juros: {formatCurrency(invoice.lateInterestAppliedAmount)} · Rotativo: {formatCurrency(invoice.revolvingInterestAppliedAmount)}</small>
                    </div>
                  ) : null}

                  <div className={styles.invoiceFooter}>
                    <div>
                      <span>Ultimo pagamento</span>
                      <strong>{formatDate(invoice.paidAtUtc)}</strong>
                    </div>
                    <div className={styles.paymentMeta}>
                      <small>Conta usada: {invoice.paidFromFinancialAccountId ? "registrada" : "nenhuma ainda"}</small>
                      <div className={styles.invoiceActions}>
                        <Link
                          className={`${styles.secondaryButton} ${styles.invoiceLink}`}
                          href={`/credit-cards?creditCardId=${invoice.creditCardId}`}
                        >
                          Ver este cartao no extrato
                        </Link>
                        {invoice.status !== "paid" ? (
                          <button className={styles.secondaryButton} onClick={() => handleOpenAdjustmentModal(invoice)}>
                            Ajustar fatura
                          </button>
                        ) : null}
                        {invoice.status === "open" ? (
                          <button className={styles.secondaryButton} onClick={() => void handleCloseInvoice(invoice)} disabled={closingInvoiceId === invoice.id}>
                            {closingInvoiceId === invoice.id ? "Fechando..." : "Fechar fatura"}
                          </button>
                        ) : null}
                        {invoice.remainingAmount > 0 ? (
                          <button className={styles.secondaryButton} onClick={() => handleOpenPaymentModal(invoice)} disabled={financialAccounts.length === 0}>Pagar fatura</button>
                        ) : null}
                      </div>
                    </div>
                  </div>

                  <section className={styles.expenseSection}>
                    <div className={styles.expenseHeader}>
                      <div>
                        <h4>Lancamentos da fatura</h4>
                        <p>Inspecao operacional das compras reais que formam este total.</p>
                      </div>
                      <span className={styles.expenseCount}>{invoiceExpenses.length} compra(s)</span>
                    </div>

                    {invoiceExpenses.length === 0 ? (
                      <div className={styles.expenseEmpty}>
                        Nenhuma compra registrada nesta fatura ainda.
                      </div>
                    ) : (
                      <div className={styles.expenseList}>
                        {invoiceExpenses.map((expense) => (
                          <article className={styles.expenseCard} key={expense.id}>
                            <div className={styles.expenseTopRow}>
                              <div>
                                <h5>{expense.transactionCategoryName}</h5>
                                <p>{expense.description ?? "Compra sem descricao"}</p>
                              </div>
                              <strong>{formatCurrency(expense.amount)}</strong>
                            </div>
                            <div className={styles.expenseMetaRow}>
                              <span>{formatInstallmentLabel(expense)}</span>
                              <span>Data da compra: {formatDate(expense.occurredOn)}</span>
                              <span>Registrada em: {formatDate(expense.createdAtUtc)}</span>
                            </div>
                          </article>
                        ))}
                      </div>
                    )}
                  </section>
                </article>
              );
            })}
          </div>
        ) : null}
      </section>

      {isCreateModalOpen ? (
        <div className={styles.modalOverlay} role="presentation">
          <div className={styles.modalCard}>
            <div className={styles.modalHeader}>
              <div>
                <p className={styles.eyebrow}>Nova fatura</p>
                <h2>Abra um ciclo de fatura</h2>
                <p>Selecione o cartao e o mes de referencia para abrir a estrutura da fatura.</p>
              </div>
              <button className={styles.iconButton} onClick={handleCloseCreateModal}>Fechar</button>
            </div>
            <form className={styles.form} onSubmit={handleCreateSubmit}>
              <label className={styles.field}>
                <span>Cartao</span>
                <select value={creditCardId} onChange={(event) => setCreditCardId(event.target.value)}>
                  <option value="">Selecione um cartao</option>
                  {creditCards.map((card) => <option key={card.id} value={card.id}>{card.name}</option>)}
                </select>
              </label>
              <label className={styles.field}>
                <span>Mes de referencia</span>
                <input type="month" value={monthValue} onChange={(event) => setMonthValue(event.target.value)} />
              </label>
              {submitError ? <div className={styles.feedbackError}>{submitError}</div> : null}
              <div className={styles.formActions}>
                <button className={styles.secondaryButton} type="button" onClick={handleCloseCreateModal} disabled={isSubmitting}>Cancelar</button>
                <button className={styles.primaryButton} type="submit" disabled={isSubmitting}>{isSubmitting ? "Abrindo..." : "Abrir fatura"}</button>
              </div>
            </form>
          </div>
        </div>
      ) : null}

      {isExpenseModalOpen ? (
        <div className={styles.modalOverlay} role="presentation">
          <div className={styles.modalCard}>
            <div className={styles.modalHeader}>
              <div>
                <p className={styles.eyebrow}>Nova compra</p>
                <h2>Registrar compra no cartao</h2>
                <p>O sistema usa a data da compra e o ciclo do cartao para criar ou atualizar automaticamente a fatura correta.</p>
              </div>
              <button className={styles.iconButton} onClick={handleCloseExpenseModal}>Fechar</button>
            </div>
            <form className={styles.form} onSubmit={handleExpenseSubmit}>
              <label className={styles.field}>
                <span>Cartao</span>
                <select value={expenseForm.creditCardId} onChange={(event) => setExpenseForm((current) => ({ ...current, creditCardId: event.target.value }))}>
                  <option value="">Selecione um cartao</option>
                  {creditCards.map((card) => <option key={card.id} value={card.id}>{card.name}</option>)}
                </select>
              </label>
              <label className={styles.field}>
                <span>Categoria</span>
                <select value={expenseForm.transactionCategoryId} onChange={(event) => setExpenseForm((current) => ({ ...current, transactionCategoryId: event.target.value }))}>
                  <option value="">Selecione uma categoria</option>
                  {expenseCategories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
                </select>
              </label>
              <div className={styles.fieldRow}>
                <label className={styles.field}>
                  <span>Valor total</span>
                  <input type="number" min="0" step="0.01" value={expenseForm.amount} onChange={(event) => setExpenseForm((current) => ({ ...current, amount: Number(event.target.value || 0) }))} />
                </label>
                <label className={styles.field}>
                  <span>Data da compra</span>
                  <input type="date" value={expenseForm.occurredOn} onChange={(event) => setExpenseForm((current) => ({ ...current, occurredOn: event.target.value }))} />
                </label>
              </div>
              <div className={styles.fieldRow}>
                <label className={styles.field}>
                  <span>Parcelas</span>
                  <input type="number" min="1" max="12" step="1" value={expenseForm.installmentCount} onChange={(event) => setExpenseForm((current) => ({ ...current, installmentCount: Number(event.target.value || 1) }))} />
                  <small className={styles.fieldHint}>De 1 a 12. O sistema distribui cada parcela nas faturas futuras do cartao.</small>
                </label>
                {expenseDestinationContext ? (
                  <label className={styles.field}>
                    <span>Destino desta compra</span>
                    <select value={expenseForm.targetInvoiceId ?? ""} onChange={(event) => setExpenseForm((current) => ({ ...current, targetInvoiceId: event.target.value || undefined }))}>
                      <option value="">Mover para a proxima fatura ({expenseDestinationContext.nextReferenceLabel})</option>
                      <option value={expenseDestinationContext.naturalInvoice.id}>Incluir na fatura fechada {expenseDestinationContext.naturalReferenceLabel}</option>
                    </select>
                    <small className={styles.fieldHint}>A compra cairia naturalmente na fatura {expenseDestinationContext.naturalReferenceLabel}, que ja esta fechada. Escolha aqui qual fatura deve refletir a realidade.</small>
                  </label>
                ) : null}
              </div>
              <label className={styles.field}>
                <span>Descricao</span>
                <textarea rows={4} value={expenseForm.description ?? ""} onChange={(event) => setExpenseForm((current) => ({ ...current, description: event.target.value }))} placeholder="Compra principal do ciclo" />
              </label>
              {submitError ? <div className={styles.feedbackError}>{submitError}</div> : null}
              <div className={styles.formActions}>
                <button className={styles.secondaryButton} type="button" onClick={handleCloseExpenseModal} disabled={isSubmitting}>Cancelar</button>
                <button className={styles.primaryButton} type="submit" disabled={isSubmitting}>{isSubmitting ? "Registrando..." : "Registrar compra"}</button>
              </div>
            </form>
          </div>
        </div>
      ) : null}


      {isAdjustmentModalOpen && adjustmentInvoice ? (
        <div className={styles.modalOverlay} role="presentation">
          <div className={styles.modalCard}>
            <div className={styles.modalHeader}>
              <div>
                <p className={styles.eyebrow}>Ajuste de fatura</p>
                <h2>Ajustar fatura</h2>
                <p>Use credito, desconto, encargos ou correcao manual controlada para refletir a realidade da fatura sem editar historico bruto.</p>
              </div>
              <button className={styles.iconButton} onClick={handleCloseAdjustmentModal}>Fechar</button>
            </div>
            <form className={styles.form} onSubmit={handleAdjustmentSubmit}>
              <div className={styles.paymentSummary}>
                <span>Fatura</span>
                <strong>{adjustmentInvoice.creditCardName} · {formatReferenceMonth(adjustmentInvoice.referenceYear, adjustmentInvoice.referenceMonth)}</strong>
                <span>Total atual</span>
                <strong>{formatCurrency(adjustmentInvoice.totalAmount)}</strong>
                <span>Ja pago</span>
                <strong>{formatCurrency(adjustmentInvoice.paidAmount)}</strong>
                <span>Saldo remanescente</span>
                <strong>{formatCurrency(adjustmentInvoice.remainingAmount)}</strong>
              </div>
              <label className={styles.field}>
                <span>Tipo de ajuste</span>
                <select value={adjustmentType} onChange={(event) => setAdjustmentType(event.target.value as InvoiceAdjustmentType)}>
                  {adjustmentTypeOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
                </select>
                <small className={styles.fieldHint}>{selectedAdjustmentTypeOption.hint}</small>
              </label>
              <label className={styles.field}>
                <span>Valor do ajuste</span>
                <input type="number" min="0" step="0.01" value={adjustmentAmount} onChange={(event) => setAdjustmentAmount(Number(event.target.value || 0))} />
                <small className={styles.fieldHint}>Use um valor positivo. O tipo escolhido define se ele entra como acrescimo ou reducao controlada.</small>
              </label>
              {submitError ? <div className={styles.feedbackError}>{submitError}</div> : null}
              <div className={styles.formActions}>
                <button className={styles.secondaryButton} type="button" onClick={handleCloseAdjustmentModal} disabled={isSubmitting}>Cancelar</button>
                <button className={styles.primaryButton} type="submit" disabled={isSubmitting}>{isSubmitting ? "Aplicando..." : "Aplicar ajuste"}</button>
              </div>
            </form>
          </div>
        </div>
      ) : null}
      {isPaymentModalOpen && selectedInvoice ? (
        <div className={styles.modalOverlay} role="presentation">
          <div className={styles.modalCard}>
            <div className={styles.modalHeader}>
              <div>
                <p className={styles.eyebrow}>Pagamento</p>
                <h2>Pagar fatura</h2>
                <p>O valor ja vem preenchido com o minimo sugerido, mas voce pode corrigir para refletir o valor real pago no banco.</p>
              </div>
              <button className={styles.iconButton} onClick={handleClosePaymentModal}>Fechar</button>
            </div>
            <form className={styles.form} onSubmit={handlePaymentSubmit}>
              <label className={styles.field}>
                <span>Conta financeira</span>
                <select value={paymentAccountId} onChange={(event) => setPaymentAccountId(event.target.value)}>
                  <option value="">Selecione uma conta</option>
                  {financialAccounts.map((account) => <option key={account.id} value={account.id}>{account.name}</option>)}
                </select>
              </label>
              <div className={styles.paymentSummary}>
                <span>Fatura</span>
                <strong>{selectedInvoice.creditCardName} · {formatReferenceMonth(selectedInvoice.referenceYear, selectedInvoice.referenceMonth)}</strong>
                <span>Total atual</span>
                <strong>{formatCurrency(selectedInvoice.totalAmount)}</strong>
                <span>Saldo remanescente</span>
                <strong>{formatCurrency(selectedInvoice.remainingAmount)}</strong>
                <span>Minimo sugerido</span>
                <strong>{formatCurrency(selectedInvoice.suggestedMinimumPaymentAmount)}</strong>
              </div>
              <label className={styles.field}>
                <span>Valor pago</span>
                <input type="number" min="0" step="0.01" value={paymentAmount} onChange={(event) => setPaymentAmount(Number(event.target.value || 0))} />
                <small className={styles.fieldHint}>Edite este valor se o banco mostrar um numero diferente do minimo sugerido.</small>
              </label>
              {submitError ? <div className={styles.feedbackError}>{submitError}</div> : null}
              <div className={styles.formActions}>
                <button className={styles.secondaryButton} type="button" onClick={handleClosePaymentModal} disabled={isSubmitting}>Cancelar</button>
                <button className={styles.primaryButton} type="submit" disabled={isSubmitting}>{isSubmitting ? "Pagando..." : "Registrar pagamento"}</button>
              </div>
            </form>
          </div>
        </div>
      ) : null}
    </main>
  );
}




















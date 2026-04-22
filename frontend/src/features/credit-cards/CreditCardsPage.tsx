"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useAuth } from "@/features/auth/AuthProvider";
import { ApiError } from "@/services/api-client";
import {
  createCreditCard,
  getCreditCardOverview,
  getCreditCards,
} from "@/services/credit-cards-service";
import { getCardExpenses, getInvoices } from "@/services/invoices-service";
import type {
  CreateCreditCardInput,
  CreditCard,
  CreditCardOverview,
} from "@/types/credit-cards";
import type { CreditCardExpense, Invoice, InvoiceStatus } from "@/types/invoices";
import styles from "./CreditCardsPage.module.css";

const INITIAL_FORM: CreateCreditCardInput = {
  name: "",
  brand: "",
  creditLimit: 0,
  closingDay: 1,
  dueDay: 1,
  description: "",
};

type StatementFilterStatus = "all" | InvoiceStatus;

type StatementFilters = {
  from: string;
  to: string;
  status: StatementFilterStatus;
};

const INITIAL_STATEMENT_FILTERS: StatementFilters = {
  from: "",
  to: "",
  status: "all",
};

function formatCurrency(value: number) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value);
}

function formatDayLabel(day: number) {
  return `${day.toString().padStart(2, "0")}`;
}

function formatReference(year: number | null, month: number | null) {
  if (!year || !month) {
    return "Sem fatura ainda";
  }

  return `${month.toString().padStart(2, "0")}/${year}`;
}

function formatDate(value: string | null) {
  if (!value) {
    return "Sem compra ainda";
  }

  const iso = value.includes("T") ? value : `${value}T00:00:00`;
  return new Intl.DateTimeFormat("pt-BR").format(new Date(iso));
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
}

function normalizeDate(value: string) {
  return value.slice(0, 10);
}

export function CreditCardsPage() {
  const { logout, status, user } = useAuth();
  const [focusedCreditCardId, setFocusedCreditCardId] = useState<string>("");
  const [creditCards, setCreditCards] = useState<CreditCard[]>([]);
  const [overview, setOverview] = useState<CreditCardOverview[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [expenses, setExpenses] = useState<CreditCardExpense[]>([]);
  const [statementFiltersByCard, setStatementFiltersByCard] = useState<Record<string, StatementFilters>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [formState, setFormState] = useState<CreateCreditCardInput>(INITIAL_FORM);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitSuccess, setSubmitSuccess] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadCreditCards = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);

    try {
      const [cards, overviewData, invoiceData, expenseData] = await Promise.all([
        getCreditCards(),
        getCreditCardOverview(),
        getInvoices(),
        getCardExpenses(),
      ]);
      setCreditCards(cards);
      setOverview(overviewData);
      setInvoices(invoiceData);
      setExpenses(expenseData);
      setStatementFiltersByCard((current) => {
        const next = { ...current };
        for (const card of cards) {
          next[card.id] ??= INITIAL_STATEMENT_FILTERS;
        }
        return next;
      });
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      const message =
        error instanceof ApiError
          ? error.message
          : "Nao foi possivel carregar seus cartoes agora.";

      setLoadError(message);
    } finally {
      setIsLoading(false);
    }
  }, [logout]);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const nextCreditCardId = params.get("creditCardId") ?? "";
    setFocusedCreditCardId(nextCreditCardId);
  }, []);

  useEffect(() => {
    if (status !== "authenticated") {
      return;
    }

    void loadCreditCards();
  }, [status, loadCreditCards]);

  useEffect(() => {
    if (!focusedCreditCardId || isLoading || creditCards.length === 0) {
      return;
    }

    const element = document.getElementById(`credit-card-${focusedCreditCardId}`);
    if (!element) {
      return;
    }

    requestAnimationFrame(() => {
      element.scrollIntoView({ behavior: "smooth", block: "center" });
    });
  }, [focusedCreditCardId, isLoading, creditCards]);

  function handleOpenModal() {
    setFormState(INITIAL_FORM);
    setSubmitError(null);
    setSubmitSuccess(null);
    setIsModalOpen(true);
  }

  function handleCloseModal() {
    if (isSubmitting) {
      return;
    }

    setIsModalOpen(false);
    setSubmitError(null);
  }

  function handleChange<K extends keyof CreateCreditCardInput>(
    field: K,
    value: CreateCreditCardInput[K],
  ) {
    setFormState((current) => ({
      ...current,
      [field]: value,
    }));
  }

  function handleStatementFilterChange(
    creditCardId: string,
    field: keyof StatementFilters,
    value: string,
  ) {
    setStatementFiltersByCard((current) => ({
      ...current,
      [creditCardId]: {
        ...(current[creditCardId] ?? INITIAL_STATEMENT_FILTERS),
        [field]: value,
      },
    }));
  }

  function handleClearStatementFilters(creditCardId: string) {
    setStatementFiltersByCard((current) => ({
      ...current,
      [creditCardId]: INITIAL_STATEMENT_FILTERS,
    }));
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitError(null);
    setSubmitSuccess(null);

    if (!formState.name.trim()) {
      setSubmitError("O nome do cartao e obrigatorio.");
      return;
    }

    if (formState.creditLimit < 0) {
      setSubmitError("O limite do cartao nao pode ser negativo.");
      return;
    }

    if (formState.closingDay < 1 || formState.closingDay > 31) {
      setSubmitError("O dia de fechamento deve estar entre 1 e 31.");
      return;
    }

    if (formState.dueDay < 1 || formState.dueDay > 31) {
      setSubmitError("O dia de vencimento deve estar entre 1 e 31.");
      return;
    }

    setIsSubmitting(true);

    try {
      const created = await createCreditCard({
        ...formState,
        brand: formState.brand?.trim() || undefined,
        description: formState.description?.trim() || undefined,
      });

      setCreditCards((current) =>
        [...current, created].sort((left, right) => left.name.localeCompare(right.name)),
      );
      await loadCreditCards();
      setSubmitSuccess("Cartao criado com sucesso.");
      setIsModalOpen(false);
      setFormState(INITIAL_FORM);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      const message =
        error instanceof ApiError
          ? error.message
          : "Nao foi possivel criar o cartao agora.";

      setSubmitError(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  const totalLimit = overview.reduce((sum, card) => sum + card.creditLimit, 0);
  const activeCards = overview.filter((card) => card.isActive).length;
  const totalOpenInvoiceAmount = overview.reduce((sum, card) => sum + card.openInvoiceAmount, 0);
  const totalPurchases = overview.reduce((sum, card) => sum + card.totalPurchasesCount, 0);
  const overviewMap = useMemo(() => new Map(overview.map((item) => [item.creditCardId, item])), [overview]);
  const invoiceMap = useMemo(() => new Map(invoices.map((invoice) => [invoice.id, invoice])), [invoices]);
  const expensesByCard = useMemo(() => {
    const map = new Map<string, CreditCardExpense[]>();
    for (const expense of expenses) {
      const current = map.get(expense.creditCardId) ?? [];
      current.push(expense);
      map.set(expense.creditCardId, current);
    }
    for (const [, items] of map) {
      items.sort((left, right) => {
        const dateCompare = new Date(right.occurredOn).getTime() - new Date(left.occurredOn).getTime();
        if (dateCompare !== 0) {
          return dateCompare;
        }
        return new Date(right.createdAtUtc).getTime() - new Date(left.createdAtUtc).getTime();
      });
    }
    return map;
  }, [expenses]);

  return (
    <main className={styles.main}>
      <header className={styles.header}>
        <div>
          <p className={styles.eyebrow}>Fase 3</p>
          <h1>Cartoes de credito</h1>
          <p className={styles.subtitle}>
            Veja a leitura inicial da situacao do credito com limite, faturas e compras
            ja vinculadas a cada cartao, sem abrir complexidade bancaria maior.
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
          <button className={styles.primaryButton} onClick={handleOpenModal}>
            Novo cartao
          </button>
        </div>
      </header>

      <section className={styles.summaryGrid}>
        <article className={styles.summaryCard}>
          <span>Total de cartoes</span>
          <strong>{overview.length}</strong>
          <small>Primeiro modulo estrutural da fase</small>
        </article>

        <article className={styles.summaryCard}>
          <span>Cartoes ativos</span>
          <strong>{activeCards}</strong>
          <small>Base viva do dominio de credito</small>
        </article>

        <article className={styles.summaryCard}>
          <span>Limite consolidado</span>
          <strong>{formatCurrency(totalLimit)}</strong>
          <small>Leitura simples do credito cadastrado</small>
        </article>

        <article className={styles.summaryCard}>
          <span>Em aberto nas faturas</span>
          <strong>{formatCurrency(totalOpenInvoiceAmount)}</strong>
          <small>Posicao atual de faturas abertas</small>
        </article>

        <article className={styles.summaryCard}>
          <span>Compras registradas</span>
          <strong>{totalPurchases}</strong>
          <small>Compras reais vinculadas aos cartoes</small>
        </article>
      </section>

      {submitSuccess ? <div className={styles.feedbackSuccess}>{submitSuccess}</div> : null}

      <section className={styles.listCard}>
        <div className={styles.listHeader}>
          <div>
            <h2>Seus cartoes</h2>
            <p>Consulte limite, posicao de faturas, quantidade de compras, ultimo movimento e agora tambem o extrato operacional de cada cartao.</p>
          </div>

          <button className={styles.secondaryButton} onClick={() => void loadCreditCards()}>
            Recarregar lista
          </button>
        </div>

        {status === "loading" || isLoading ? (
          <div className={styles.skeletonList}>
            <div className={styles.skeletonRow} />
            <div className={styles.skeletonRow} />
            <div className={styles.skeletonRow} />
          </div>
        ) : null}

        {status !== "loading" && !isLoading && loadError ? (
          <div className={styles.stateBlock}>
            <h3>Nao foi possivel carregar seus cartoes.</h3>
            <p>{loadError}</p>
            <button className={styles.secondaryButton} onClick={() => void loadCreditCards()}>
              Tentar novamente
            </button>
          </div>
        ) : null}

        {status !== "loading" && !isLoading && !loadError && creditCards.length === 0 ? (
          <div className={styles.stateBlock}>
            <h3>Nenhum cartao cadastrado ainda</h3>
            <p>
              Voce ainda nao possui cartoes cadastrados. Cadastre seu primeiro
              cartao para preparar o controle de faturas e compras no credito.
            </p>
            <button className={styles.primaryButton} onClick={handleOpenModal}>
              Novo cartao
            </button>
          </div>
        ) : null}

        {status !== "loading" && !isLoading && !loadError && creditCards.length > 0 ? (
          <div className={styles.cardList}>
            {creditCards.map((creditCard) => {
              const currentOverview = overviewMap.get(creditCard.id);
              const cardExpenses = expensesByCard.get(creditCard.id) ?? [];
              const filters = statementFiltersByCard[creditCard.id] ?? INITIAL_STATEMENT_FILTERS;
              const filteredExpenses = cardExpenses.filter((expense) => {
                const invoice = invoiceMap.get(expense.invoiceId);
                const expenseDate = normalizeDate(expense.occurredOn);
                const matchesFrom = !filters.from || expenseDate >= filters.from;
                const matchesTo = !filters.to || expenseDate <= filters.to;
                const matchesStatus = filters.status === "all" || invoice?.status === filters.status;
                return matchesFrom && matchesTo && matchesStatus;
              });
              const statementGroups = Array.from(
                filteredExpenses.reduce((map, expense) => {
                  const current = map.get(expense.invoiceId) ?? [];
                  current.push(expense);
                  map.set(expense.invoiceId, current);
                  return map;
                }, new Map<string, CreditCardExpense[]>()),
              ).sort(([leftInvoiceId], [rightInvoiceId]) => {
                const leftInvoice = invoiceMap.get(leftInvoiceId);
                const rightInvoice = invoiceMap.get(rightInvoiceId);
                const leftDate = leftInvoice ? new Date(leftInvoice.createdAtUtc).getTime() : 0;
                const rightDate = rightInvoice ? new Date(rightInvoice.createdAtUtc).getTime() : 0;
                return rightDate - leftDate;
              });
              const filteredTotal = filteredExpenses.reduce((sum, expense) => sum + expense.amount, 0);
              const hasActiveFilters = Boolean(filters.from || filters.to || filters.status !== "all");

              return (
                <article
                  className={`${styles.creditCard} ${creditCard.id === focusedCreditCardId ? styles.creditCardHighlight : ""}`}
                  id={`credit-card-${creditCard.id}`}
                  key={creditCard.id}
                >
                  <div className={styles.cardTopRow}>
                    <div>
                      <h3>{creditCard.name}</h3>
                      <p>{creditCard.brand ?? "Bandeira nao informada"}</p>
                    </div>

                    <span className={styles.statusBadge}>
                      {creditCard.isActive ? "Ativo" : "Inativo"}
                    </span>
                  </div>

                  <div className={styles.cardMetaRow}>
                    <div>
                      <span>Limite</span>
                      <strong>{formatCurrency(creditCard.creditLimit)}</strong>
                    </div>

                    <div>
                      <span>Fechamento</span>
                      <strong>Dia {formatDayLabel(creditCard.closingDay)}</strong>
                    </div>

                    <div>
                      <span>Vencimento</span>
                      <strong>Dia {formatDayLabel(creditCard.dueDay)}</strong>
                    </div>
                  </div>

                  <div className={styles.overviewGrid}>
                    <div className={styles.overviewItem}>
                      <span>Faturas abertas</span>
                      <strong>{currentOverview?.openInvoicesCount ?? 0}</strong>
                    </div>
                    <div className={styles.overviewItem}>
                      <span>Valor em aberto</span>
                      <strong>{formatCurrency(currentOverview?.openInvoiceAmount ?? 0)}</strong>
                    </div>
                    <div className={styles.overviewItem}>
                      <span>Total de compras</span>
                      <strong>{currentOverview?.totalPurchasesCount ?? 0}</strong>
                    </div>
                    <div className={styles.overviewItem}>
                      <span>Compras acumuladas</span>
                      <strong>{formatCurrency(currentOverview?.totalPurchasesAmount ?? 0)}</strong>
                    </div>
                    <div className={styles.overviewItem}>
                      <span>Ultima fatura</span>
                      <strong>{formatReference(currentOverview?.latestInvoiceReferenceYear ?? null, currentOverview?.latestInvoiceReferenceMonth ?? null)}</strong>
                    </div>
                    <div className={styles.overviewItem}>
                      <span>Ultima compra</span>
                      <strong>{formatDate(currentOverview?.lastPurchaseOn ?? null)}</strong>
                    </div>
                  </div>

                  {creditCard.description ? (
                    <p className={styles.cardDescription}>{creditCard.description}</p>
                  ) : null}

                  <section className={styles.statementSection}>
                    <div className={styles.statementHeader}>
                      <div>
                        <h4>Extrato do cartao</h4>
                        <p>Leitura detalhada das compras reais, agrupadas por fatura para facilitar a inspecao operacional.</p>
                      </div>
                      <span className={styles.statementCount}>{filteredExpenses.length} lancamento(s)</span>
                    </div>

                    <div className={styles.statementFilters}>
                      <label className={styles.filterField}>
                        <span>De</span>
                        <input
                          type="date"
                          value={filters.from}
                          onChange={(event) => handleStatementFilterChange(creditCard.id, "from", event.target.value)}
                        />
                      </label>
                      <label className={styles.filterField}>
                        <span>Ate</span>
                        <input
                          type="date"
                          value={filters.to}
                          onChange={(event) => handleStatementFilterChange(creditCard.id, "to", event.target.value)}
                        />
                      </label>
                      <label className={styles.filterField}>
                        <span>Status da fatura</span>
                        <select
                          value={filters.status}
                          onChange={(event) => handleStatementFilterChange(creditCard.id, "status", event.target.value)}
                        >
                          <option value="all">Todos</option>
                          <option value="open">Aberta</option>
                          <option value="paid">Paga</option>
                          <option value="closed">Fechada</option>
                        </select>
                      </label>
                      <div className={styles.filterSummary}>
                        <span>Total filtrado</span>
                        <strong>{formatCurrency(filteredTotal)}</strong>
                      </div>
                      <button
                        className={styles.secondaryButton}
                        type="button"
                        onClick={() => handleClearStatementFilters(creditCard.id)}
                        disabled={!hasActiveFilters}
                      >
                        Limpar filtros
                      </button>
                    </div>

                    {statementGroups.length === 0 ? (
                      <div className={styles.statementEmpty}>
                        {hasActiveFilters
                          ? "Nenhum lancamento encontrado com os filtros atuais."
                          : "Nenhuma compra registrada neste cartao ainda."}
                      </div>
                    ) : (
                      <div className={styles.statementGroupList}>
                        {statementGroups.map(([invoiceId, groupExpenses]) => {
                          const invoice = invoiceMap.get(invoiceId);
                          const subtotal = groupExpenses.reduce((sum, expense) => sum + expense.amount, 0);

                          return (
                            <section className={styles.statementGroup} key={invoiceId}>
                              <div className={styles.statementGroupHeader}>
                                <div>
                                  <h5>{invoice ? `Fatura ${formatReference(invoice.referenceYear, invoice.referenceMonth)}` : "Fatura nao encontrada"}</h5>
                                  <p>
                                    {invoice
                                      ? `Status ${invoice.status} · fechamento ${formatDate(invoice.closingDate)} · vencimento ${formatDate(invoice.dueDate)}`
                                      : "Lancamentos sem contexto de fatura disponivel."}
                                  </p>
                                </div>
                                <strong>{formatCurrency(subtotal)}</strong>
                              </div>

                              <div className={styles.statementGroupActions}>
                                <Link
                                  className={`${styles.secondaryButton} ${styles.statementLink}`}
                                  href={`/invoices?creditCardId=${creditCard.id}&invoiceId=${invoiceId}`}
                                >
                                  Abrir esta fatura
                                </Link>
                              </div>

                              <div className={styles.statementExpenseList}>
                                {groupExpenses.map((expense) => (
                                  <article className={styles.statementExpenseCard} key={expense.id}>
                                    <div className={styles.statementExpenseTopRow}>
                                      <div>
                                        <h6>{expense.transactionCategoryName}</h6>
                                        <p>{expense.description ?? "Compra sem descricao"}</p>
                                      </div>
                                      <strong>{formatCurrency(expense.amount)}</strong>
                                    </div>
                                    <div className={styles.statementExpenseMetaRow}>
                                      <span>Compra em {formatDate(expense.occurredOn)}</span>
                                      <span>Registro em {formatDateTime(expense.createdAtUtc)}</span>
                                    </div>
                                  </article>
                                ))}
                              </div>
                            </section>
                          );
                        })}
                      </div>
                    )}
                  </section>
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
                <p className={styles.eyebrow}>Novo cartao</p>
                <h2>Cadastre um cartao de credito</h2>
                <p>
                  Crie um cartao com limite e ciclo basico para preparar o controle
                  de faturas nas proximas entregas.
                </p>
              </div>

              <button className={styles.iconButton} onClick={handleCloseModal}>
                Fechar
              </button>
            </div>

            <form className={styles.form} onSubmit={handleSubmit}>
              <label className={styles.field}>
                <span>Nome</span>
                <input
                  value={formState.name}
                  onChange={(event) => handleChange("name", event.target.value)}
                  placeholder="Cartao principal"
                />
              </label>

              <div className={styles.fieldRow}>
                <label className={styles.field}>
                  <span>Limite</span>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={formState.creditLimit}
                    onChange={(event) =>
                      handleChange("creditLimit", Number(event.target.value || 0))
                    }
                  />
                </label>

                <label className={styles.field}>
                  <span>Bandeira</span>
                  <input
                    value={formState.brand ?? ""}
                    onChange={(event) => handleChange("brand", event.target.value)}
                    placeholder="Visa"
                  />
                </label>
              </div>

              <div className={styles.fieldRow}>
                <label className={styles.field}>
                  <span>Dia de fechamento</span>
                  <input
                    type="number"
                    min="1"
                    max="31"
                    value={formState.closingDay}
                    onChange={(event) =>
                      handleChange("closingDay", Number(event.target.value || 1))
                    }
                  />
                </label>

                <label className={styles.field}>
                  <span>Dia de vencimento</span>
                  <input
                    type="number"
                    min="1"
                    max="31"
                    value={formState.dueDay}
                    onChange={(event) =>
                      handleChange("dueDay", Number(event.target.value || 1))
                    }
                  />
                </label>
              </div>

              <label className={styles.field}>
                <span>Descricao</span>
                <textarea
                  value={formState.description ?? ""}
                  onChange={(event) => handleChange("description", event.target.value)}
                  rows={4}
                  placeholder="Cartao usado nas despesas recorrentes"
                />
              </label>

              {submitError ? <div className={styles.feedbackError}>{submitError}</div> : null}

              <div className={styles.formActions}>
                <button
                  className={styles.secondaryButton}
                  type="button"
                  onClick={handleCloseModal}
                  disabled={isSubmitting}
                >
                  Cancelar
                </button>

                <button className={styles.primaryButton} type="submit" disabled={isSubmitting}>
                  {isSubmitting ? "Criando..." : "Criar cartao"}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}
    </main>
  );
}

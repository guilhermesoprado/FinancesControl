"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useAuth } from "@/features/auth/AuthProvider";
import { SharedSkeletonRows, SharedState } from "@/features/shared-state/SharedState";
import { ApiError } from "@/services/api-client";
import { getFinancialOverview } from "@/services/financial-overview-service";
import type {
  FinancialOverview,
  FinancialOverviewAccountType,
  FinancialOverviewTransactionType,
} from "@/types/financial-overview";
import styles from "./FinancialOverviewPage.module.css";

type FinancialOverviewTransaction = FinancialOverview["recentTransactions"][number];

const ACCOUNT_TYPE_LABELS: Record<FinancialOverviewAccountType, string> = {
  bank_account: "Conta bancaria",
  wallet: "Carteira",
  investment_account: "Conta de investimento",
};

const TRANSACTION_TYPE_LABELS: Record<FinancialOverviewTransactionType, string> = {
  income: "Receita",
  expense: "Despesa",
  transfer: "Transferencia",
};

function formatCurrency(value: number) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value);
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(new Date(`${value}T00:00:00`));
}

function formatPeriod(from: string, to: string) {
  return `${formatDate(from)} ate ${formatDate(to)}`;
}

function formatPercentage(value: number) {
  return new Intl.NumberFormat("pt-BR", {
    style: "percent",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value);
}

function formatSignedCurrency(value: number) {
  const formatted = formatCurrency(Math.abs(value));
  return value > 0 ? `+${formatted}` : value < 0 ? `-${formatted}` : formatted;
}

export function FinancialOverviewPage() {
  const { logout, status, user } = useAuth();
  const [overview, setOverview] = useState<FinancialOverview | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const loadOverview = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);

    try {
      const data = await getFinancialOverview();
      setOverview(data);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      setLoadError(
        error instanceof ApiError
          ? error.message
          : "Nao foi possivel carregar sua visao financeira agora.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [logout]);

  useEffect(() => {
    if (status !== "authenticated") {
      return;
    }

    void loadOverview();
  }, [status, loadOverview]);

  const accountNames = useMemo(
    () => new Map((overview?.accounts ?? []).map((account) => [account.id, account.name])),
    [overview],
  );

  const managerialReading = useMemo(() => {
    if (!overview) {
      return null;
    }

    const netResult = overview.incomeTotal - overview.expenseTotal;
    const periodComparison = overview.periodComparison ?? {
      previousPeriodFrom: overview.periodFrom,
      previousPeriodTo: overview.periodTo,
      previousIncomeTotal: 0,
      previousExpenseTotal: 0,
      previousTransferTotal: 0,
      previousNetResult: 0,
    };
    const previousNetResult = periodComparison.previousNetResult;
    const topAccount = [...overview.accounts].sort(
      (left, right) => right.visibleBalance - left.visibleBalance,
    )[0] ?? null;
    const topAccountShare =
      topAccount && overview.consolidatedBalance > 0
        ? topAccount.visibleBalance / overview.consolidatedBalance
        : null;
    const scheduledTransactions = overview.recentTransactions.filter(
      (transaction) => transaction.status === "scheduled",
    ).length;
    const recentExpenses = overview.recentTransactions.filter(
      (transaction) => transaction.type === "expense",
    ).length;
    const recentIncome = overview.recentTransactions.filter(
      (transaction) => transaction.type === "income",
    ).length;
    const recentTransfers = overview.recentTransactions.filter(
      (transaction) => transaction.type === "transfer",
    ).length;
    const expensePressure =
      overview.incomeTotal > 0 ? overview.expenseTotal / overview.incomeTotal : null;
    const incomeDelta = overview.incomeTotal - periodComparison.previousIncomeTotal;
    const expenseDelta = overview.expenseTotal - periodComparison.previousExpenseTotal;
    const netResultDelta = netResult - previousNetResult;

    return {
      netResult,
      previousNetResult,
      periodComparison,
      topAccount,
      topAccountShare,
      scheduledTransactions,
      recentExpenses,
      recentIncome,
      recentTransfers,
      expensePressure,
      incomeDelta,
      expenseDelta,
      netResultDelta,
    };
  }, [overview]);

  const hasData =
    (overview?.accounts.length ?? 0) > 0 ||
    (overview?.recentTransactions.length ?? 0) > 0;

  function transactionDetail(transaction: FinancialOverviewTransaction) {
    if (transaction.type === "transfer") {
      const from = transaction.sourceFinancialAccountId
        ? accountNames.get(transaction.sourceFinancialAccountId)
        : null;
      const to = transaction.destinationFinancialAccountId
        ? accountNames.get(transaction.destinationFinancialAccountId)
        : null;

      return `${from ?? "Conta de origem"} para ${to ?? "Conta de destino"}`;
    }

    if (transaction.financialAccountId) {
      return accountNames.get(transaction.financialAccountId) ?? "Conta nao encontrada";
    }

    return "Conta nao informada";
  }

  return (
    <main className={styles.main}>
      <header className={styles.header}>
        <div>
          <p className={styles.eyebrow}>Fase 2</p>
          <h1>Visao financeira</h1>
          <p className={styles.subtitle}>
            Acompanhe seu panorama atual com um resumo consolidado das contas e das movimentacoes mais recentes.
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
        </div>
      </header>

      {status === "loading" || isLoading ? (
        <section className={styles.loadingBlock}>
          <SharedSkeletonRows rows={4} />
        </section>
      ) : null}

      {status !== "loading" && !isLoading && loadError ? (
        <SharedState
          tone="error"
          eyebrow="Falha na leitura"
          title="Nao foi possivel carregar sua visao financeira."
          description={loadError}
          actions={
            <button className={styles.secondaryButton} onClick={() => void loadOverview()}>
              Tentar novamente
            </button>
          }
        />
      ) : null}

      {status !== "loading" && !isLoading && !loadError && overview && !hasData ? (
        <SharedState
          tone="empty"
          eyebrow="Panorama vazio"
          title="Seu panorama financeiro ainda nao possui dados suficientes"
          description="Cadastre contas e registre movimentacoes para acompanhar aqui um resumo consolidado do seu ambiente financeiro."
          actions={
            <div className={styles.linkRow}>
              <Link className={styles.primaryLink} href="/financial-accounts">
                Ver contas
              </Link>
              <Link className={styles.secondaryLink} href="/transactions">
                Ver transacoes
              </Link>
            </div>
          }
        />
      ) : null}

      {status !== "loading" && !isLoading && !loadError && overview && hasData ? (
        <>
          <section className={styles.summaryGrid}>
            <article className={`${styles.summaryCard} ${styles.summaryLeadCard}`}>
              <span>Saldo consolidado</span>
              <strong>{formatCurrency(overview.consolidatedBalance)}</strong>
              <small>{overview.activeAccountsCount} contas ativas</small>
            </article>
            <article className={styles.summaryCard}>
              <span>Receitas do periodo</span>
              <strong>{formatCurrency(overview.incomeTotal)}</strong>
              <small>{formatPeriod(overview.periodFrom, overview.periodTo)}</small>
            </article>
            <article className={styles.summaryCard}>
              <span>Despesas do periodo</span>
              <strong>{formatCurrency(overview.expenseTotal)}</strong>
              <small>{formatPeriod(overview.periodFrom, overview.periodTo)}</small>
            </article>
            <article className={styles.summaryCard}>
              <span>Transferencias do periodo</span>
              <strong>{formatCurrency(overview.transferTotal)}</strong>
              <small>Movimentacao interna entre contas</small>
            </article>
          </section>

          {managerialReading ? (
            <section className={styles.managerialSection}>
              <div className={styles.sectionHeader}>
                <div>
                  <p className={styles.sectionEyebrow}>Fase 7</p>
                  <h2>Leitura gerencial do periodo</h2>
                  <p className={styles.sectionSubtitle}>
                    Sinais operacionais extraidos do periodo atual e das movimentacoes recentes, sem abrir um modulo analitico pesado.
                  </p>
                </div>
              </div>

              <div className={styles.managerialGrid}>
                <article className={`${styles.managerialCard} ${styles.comparisonLeadCard}`}>
                  <span>Comparacao com o periodo anterior</span>
                  <strong>{formatPeriod(managerialReading.periodComparison.previousPeriodFrom, managerialReading.periodComparison.previousPeriodTo)}</strong>
                  <p>
                    O recorte atual esta sendo comparado com um periodo anterior equivalente em duracao, para manter a leitura simples e direta.
                  </p>
                </article>

                <article className={styles.managerialCard}>
                  <span>Resultado operacional</span>
                  <strong
                    className={
                      managerialReading.netResult >= 0
                        ? styles.positiveValue
                        : styles.negativeValue
                    }
                  >
                    {formatCurrency(managerialReading.netResult)}
                  </strong>
                  <p>
                    {managerialReading.netResult >= 0
                      ? "O periodo atual esta sustentado por receitas acima das despesas registradas."
                      : "As despesas do periodo superam as receitas registradas e merecem atencao imediata."}
                  </p>
                </article>

                <article className={styles.managerialCard}>
                  <span>Maior concentracao de saldo</span>
                  <strong>
                    {managerialReading.topAccount?.name ?? "Sem conta dominante"}
                  </strong>
                  <p>
                    {managerialReading.topAccount && managerialReading.topAccountShare !== null
                      ? `${formatCurrency(managerialReading.topAccount.visibleBalance)} concentrados em ${formatPercentage(managerialReading.topAccountShare)} do saldo visivel atual.`
                      : "Ainda nao ha base suficiente para identificar concentracao relevante de saldo."}
                  </p>
                </article>

                <article className={styles.managerialCard}>
                  <span>Ritmo recente</span>
                  <strong>{managerialReading.scheduledTransactions} previstas</strong>
                  <p>
                    {managerialReading.recentExpenses} despesas, {managerialReading.recentIncome} receitas e {managerialReading.recentTransfers} transferencias nas movimentacoes mais recentes.
                  </p>
                </article>

                <article className={styles.managerialCard}>
                  <span>Pressao de despesas</span>
                  <strong>
                    {managerialReading.expensePressure !== null
                      ? formatPercentage(managerialReading.expensePressure)
                      : "Sem base"}
                  </strong>
                  <p>
                    {managerialReading.expensePressure !== null
                      ? "Parcela da receita do periodo atualmente consumida por despesas registradas."
                      : "Esse sinal depende de receitas registradas no periodo atual."}
                  </p>
                </article>
              </div>

              <div className={styles.comparisonGrid}>
                <article className={styles.comparisonCard}>
                  <span>Receitas vs periodo anterior</span>
                  <strong className={managerialReading.incomeDelta >= 0 ? styles.positiveValue : styles.negativeValue}>
                    {formatSignedCurrency(managerialReading.incomeDelta)}
                  </strong>
                  <p>
                    Atual {formatCurrency(overview.incomeTotal)} vs anterior {formatCurrency(managerialReading.periodComparison.previousIncomeTotal)}.
                  </p>
                </article>

                <article className={styles.comparisonCard}>
                  <span>Despesas vs periodo anterior</span>
                  <strong className={managerialReading.expenseDelta <= 0 ? styles.positiveValue : styles.negativeValue}>
                    {formatSignedCurrency(managerialReading.expenseDelta)}
                  </strong>
                  <p>
                    Atual {formatCurrency(overview.expenseTotal)} vs anterior {formatCurrency(managerialReading.periodComparison.previousExpenseTotal)}.
                  </p>
                </article>

                <article className={styles.comparisonCard}>
                  <span>Resultado vs periodo anterior</span>
                  <strong className={managerialReading.netResultDelta >= 0 ? styles.positiveValue : styles.negativeValue}>
                    {formatSignedCurrency(managerialReading.netResultDelta)}
                  </strong>
                  <p>
                    Atual {formatCurrency(managerialReading.netResult)} vs anterior {formatCurrency(managerialReading.previousNetResult)}.
                  </p>
                </article>
              </div>
            </section>
          ) : null}

            <section className={styles.linkRow}>
              <Link className={styles.primaryLink} href="/transactions">
                Ver transacoes
              </Link>
              <Link className={styles.secondaryLink} href="/financial-accounts">
                Ver contas
              </Link>
            </section>

          <section className={styles.intelligenceGrid}>
            <article className={styles.panelCard}>
              <div className={styles.panelHeader}>
                <div>
                  <h2>Leitura por conta no periodo</h2>
                  <p>Resultado operacional simples por conta, considerando receitas e despesas do periodo atual.</p>
                </div>
              </div>

              <div className={styles.accountSummaryList}>
                {overview.accountSummaries.length === 0 ? (
                  <div className={styles.inlineEmpty}>
                    <p>Nao ha movimentacoes suficientes para resumir contas no periodo atual.</p>
                  </div>
                ) : (
                  overview.accountSummaries.map((summary) => (
                    <article className={styles.accountSummaryCard} key={summary.accountId}>
                      <div className={styles.accountSummaryTopRow}>
                        <div>
                          <h3>{summary.accountName}</h3>
                          <p>Receitas e despesas associadas a esta conta no recorte atual.</p>
                        </div>
                        <strong
                          className={
                            summary.netResult >= 0 ? styles.positiveValue : styles.negativeValue
                          }
                        >
                          {formatCurrency(summary.netResult)}
                        </strong>
                      </div>
                      <div className={styles.accountSummaryMetaRow}>
                        <div>
                          <span>Receitas</span>
                          <strong>{formatCurrency(summary.incomeTotal)}</strong>
                        </div>
                        <div>
                          <span>Despesas</span>
                          <strong>{formatCurrency(summary.expenseTotal)}</strong>
                        </div>
                      </div>
                    </article>
                  ))
                )}
              </div>
            </article>

            <article className={styles.panelCard}>
              <div className={styles.panelHeader}>
                <div>
                  <h2>Maiores categorias do periodo</h2>
                  <p>Categorias com maior peso no recorte atual, separadas pelo tipo da movimentacao.</p>
                </div>
              </div>

              <div className={styles.categorySummaryList}>
                {overview.categorySummaries.length === 0 ? (
                  <div className={styles.inlineEmpty}>
                    <p>Nao ha categorias suficientes para resumir o periodo atual.</p>
                  </div>
                ) : (
                  overview.categorySummaries.map((summary) => (
                    <article className={styles.categorySummaryCard} key={`${summary.type}-${summary.categoryId}`}>
                      <div className={styles.categorySummaryTopRow}>
                        <div>
                          <h3>{summary.categoryName}</h3>
                          <p>{TRANSACTION_TYPE_LABELS[summary.type]}</p>
                        </div>
                        <span className={`${styles.typeBadge} ${styles[`typeBadge${summary.type}`]}`}>
                          {TRANSACTION_TYPE_LABELS[summary.type]}
                        </span>
                      </div>
                      <div className={styles.categorySummaryMetaRow}>
                        <div>
                          <span>Total</span>
                          <strong>{formatCurrency(summary.totalAmount)}</strong>
                        </div>
                        <div>
                          <span>Movimentacoes</span>
                          <strong>{summary.transactionsCount}</strong>
                        </div>
                      </div>
                    </article>
                  ))
                )}
              </div>
            </article>
          </section>

          <section className={styles.contentGrid}>
            <article className={styles.panelCard}>
              <div className={styles.panelHeader}>
                <div>
                  <h2>Contas com saldo</h2>
                  <p>Veja onde o saldo visivel esta alocado neste momento.</p>
                </div>
              </div>

              <div className={styles.accountList}>
                {overview.accounts.map((account) => (
                  <article className={styles.accountCard} key={account.id}>
                    <div>
                      <h3>{account.name}</h3>
                      <p>{account.institutionName ?? ACCOUNT_TYPE_LABELS[account.type]}</p>
                    </div>
                    <div className={styles.accountMeta}>
                      <span>{account.isActive ? "Ativa" : "Inativa"}</span>
                      <strong>{formatCurrency(account.visibleBalance)}</strong>
                    </div>
                  </article>
                ))}
              </div>
            </article>

            <article className={styles.panelCard}>
              <div className={styles.panelHeader}>
                <div>
                  <h2>Transacoes recentes</h2>
                  <p>Resumo rapido das movimentacoes mais recentes do periodo.</p>
                </div>
              </div>

              <div className={styles.transactionList}>
                {overview.recentTransactions.length === 0 ? (
                  <div className={styles.inlineEmpty}>
                    <p>Nenhuma transacao recente encontrada no periodo atual.</p>
                  </div>
                ) : (
                  overview.recentTransactions.map((transaction) => (
                    <article className={styles.transactionCard} key={transaction.id}>
                      <div className={styles.transactionTopRow}>
                        <div>
                          <h3>{TRANSACTION_TYPE_LABELS[transaction.type]}</h3>
                          <p>{transactionDetail(transaction)}</p>
                        </div>
                        <span className={`${styles.typeBadge} ${styles[`typeBadge${transaction.type}`]}`}>
                          {TRANSACTION_TYPE_LABELS[transaction.type]}
                        </span>
                      </div>
                      <div className={styles.transactionMetaRow}>
                        <div>
                          <span>Valor</span>
                          <strong>{formatCurrency(transaction.amount)}</strong>
                        </div>
                        <div>
                          <span>Data</span>
                          <strong>{formatDate(transaction.occurredOn)}</strong>
                        </div>
                        <div>
                          <span>Status</span>
                          <strong>{transaction.status === "posted" ? "Realizada" : "Prevista"}</strong>
                        </div>
                      </div>
                      {transaction.description ? (
                        <p className={styles.transactionDescription}>{transaction.description}</p>
                      ) : null}
                    </article>
                  ))
                )}
              </div>
            </article>
          </section>
        </>
      ) : null}
    </main>
  );
}

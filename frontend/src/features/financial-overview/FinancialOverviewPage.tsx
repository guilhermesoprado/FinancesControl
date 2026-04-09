"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useAuth } from "@/features/auth/AuthProvider";
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
        <section className={styles.skeletonGrid}>
          <div className={styles.skeletonCard} />
          <div className={styles.skeletonCard} />
          <div className={styles.skeletonCard} />
          <div className={styles.skeletonCard} />
        </section>
      ) : null}

      {status !== "loading" && !isLoading && loadError ? (
        <section className={styles.stateBlock}>
          <h2>Nao foi possivel carregar sua visao financeira.</h2>
          <p>{loadError}</p>
          <button className={styles.secondaryButton} onClick={() => void loadOverview()}>
            Tentar novamente
          </button>
        </section>
      ) : null}

      {status !== "loading" && !isLoading && !loadError && overview && !hasData ? (
        <section className={styles.stateBlock}>
          <h2>Seu panorama financeiro ainda nao possui dados suficientes</h2>
          <p>
            Cadastre contas e registre movimentacoes para acompanhar aqui um resumo consolidado do seu ambiente financeiro.
          </p>
          <div className={styles.linkRow}>
            <Link className={styles.primaryLink} href="/financial-accounts">
              Ver contas
            </Link>
            <Link className={styles.secondaryLink} href="/transactions">
              Ver transacoes
            </Link>
          </div>
        </section>
      ) : null}

      {status !== "loading" && !isLoading && !loadError && overview && hasData ? (
        <>
          <section className={styles.summaryGrid}>
            <article className={styles.summaryCard}>
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

          <section className={styles.linkRow}>
            <Link className={styles.primaryLink} href="/transactions">
              Ver transacoes
            </Link>
            <Link className={styles.secondaryLink} href="/financial-accounts">
              Ver contas
            </Link>
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

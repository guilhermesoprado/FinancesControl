"use client";

import { useCallback, useEffect, useState } from "react";
import { ApiError } from "@/services/api-client";
import {
  createFinancialAccount,
  getFinancialAccounts,
} from "@/services/financial-accounts-service";
import { useAuth } from "@/features/auth/AuthProvider";
import type {
  CreateFinancialAccountInput,
  FinancialAccount,
  FinancialAccountType,
} from "@/types/financial-accounts";
import styles from "./FinancialAccountsPage.module.css";

const INITIAL_FORM: CreateFinancialAccountInput = {
  name: "",
  type: "bank_account",
  initialBalance: 0,
  institutionName: "",
  description: "",
};

const TYPE_LABELS: Record<FinancialAccountType, string> = {
  bank_account: "Conta bancaria",
  wallet: "Carteira",
  investment_account: "Conta de investimento",
};

function formatCurrency(value: number | null) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value ?? 0);
}

export function FinancialAccountsPage() {
  const { logout, status, user } = useAuth();
  const [accounts, setAccounts] = useState<FinancialAccount[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [formState, setFormState] = useState<CreateFinancialAccountInput>(
    INITIAL_FORM,
  );
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitSuccess, setSubmitSuccess] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadAccounts = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);

    try {
      const data = await getFinancialAccounts();
      setAccounts(data);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      const message =
        error instanceof ApiError
          ? error.message
          : "Nao foi possivel carregar suas contas agora.";

      setLoadError(message);
    } finally {
      setIsLoading(false);
    }
  }, [logout]);

  useEffect(() => {
    if (status !== "authenticated") {
      return;
    }

    void loadAccounts();
  }, [status, loadAccounts]);

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

  function handleChange<K extends keyof CreateFinancialAccountInput>(
    field: K,
    value: CreateFinancialAccountInput[K],
  ) {
    setFormState((current) => ({
      ...current,
      [field]: value,
    }));
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitError(null);
    setSubmitSuccess(null);

    if (!formState.name.trim()) {
      setSubmitError("O nome da conta e obrigatorio.");
      return;
    }

    if (formState.initialBalance < 0) {
      setSubmitError("O saldo inicial nao pode ser negativo.");
      return;
    }

    setIsSubmitting(true);

    try {
      const created = await createFinancialAccount({
        ...formState,
        institutionName: formState.institutionName?.trim() || undefined,
        description: formState.description?.trim() || undefined,
      });

      setAccounts((current) =>
        [...current, created].sort((left, right) =>
          left.name.localeCompare(right.name),
        ),
      );
      setSubmitSuccess("Conta criada com sucesso.");
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
          : "Nao foi possivel criar a conta agora.";

      setSubmitError(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className={styles.main}>
      <header className={styles.header}>
        <div>
          <p className={styles.eyebrow}>Fase 2</p>
          <h1>Contas financeiras</h1>
          <p className={styles.subtitle}>
            Gerencie as estruturas que recebem e concentram o seu saldo
            disponivel.
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
            Nova conta
          </button>
        </div>
      </header>

      <section className={styles.summaryGrid}>
        <article className={styles.summaryCard}>
          <span>Total de contas</span>
          <strong>{accounts.length}</strong>
          <small>Primeiro modulo operacional da fase</small>
        </article>

        <article className={styles.summaryCard}>
          <span>Tipos suportados</span>
          <strong>3</strong>
          <small>Bank account, wallet e investment</small>
        </article>

        <article className={styles.summaryCard}>
          <span>Contrato backend</span>
          <strong>Estavel</strong>
          <small>POST e GET ja validados</small>
        </article>
      </section>

      {submitSuccess ? (
        <div className={styles.feedbackSuccess}>{submitSuccess}</div>
      ) : null}

      <section className={styles.listCard}>
        <div className={styles.listHeader}>
          <div>
            <h2>Suas contas</h2>
            <p>Visualize e crie as contas financeiras do seu ambiente.</p>
          </div>

          <button
            className={styles.secondaryButton}
            onClick={() => void loadAccounts()}
          >
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
            <h3>Nao foi possivel carregar suas contas.</h3>
            <p>{loadError}</p>
            <button
              className={styles.secondaryButton}
              onClick={() => void loadAccounts()}
            >
              Tentar novamente
            </button>
          </div>
        ) : null}

        {status !== "loading" && !isLoading && !loadError && accounts.length === 0 ? (
          <div className={styles.stateBlock}>
            <h3>Nenhuma conta cadastrada ainda</h3>
            <p>
              Voce ainda nao possui contas cadastradas. Crie sua primeira
              conta para comecar a registrar movimentacoes.
            </p>
            <button className={styles.primaryButton} onClick={handleOpenModal}>
              Nova conta
            </button>
          </div>
        ) : null}

        {status !== "loading" && !isLoading && !loadError && accounts.length > 0 ? (
          <div className={styles.accountList}>
            {accounts.map((account) => (
              <article className={styles.accountCard} key={account.id}>
                <div className={styles.accountTopRow}>
                  <div>
                    <h3>{account.name}</h3>
                    <p>{account.institutionName ?? "Instituicao nao informada"}</p>
                  </div>

                  <span className={styles.typeBadge}>
                    {TYPE_LABELS[account.type]}
                  </span>
                </div>

                <div className={styles.accountMetaRow}>
                  <div>
                    <span>Saldo inicial</span>
                    <strong>{formatCurrency(account.initialBalance)}</strong>
                  </div>

                  <div>
                    <span>Saldo visivel</span>
                    <strong>
                      {formatCurrency(account.currentBalanceSnapshot)}
                    </strong>
                  </div>

                  <div>
                    <span>Status</span>
                    <strong>{account.isActive ? "Ativa" : "Inativa"}</strong>
                  </div>
                </div>

                {account.description ? (
                  <p className={styles.accountDescription}>{account.description}</p>
                ) : null}
              </article>
            ))}
          </div>
        ) : null}
      </section>

      {isModalOpen ? (
        <div className={styles.modalOverlay} role="presentation">
          <div className={styles.modalCard}>
            <div className={styles.modalHeader}>
              <div>
                <p className={styles.eyebrow}>Nova conta</p>
                <h2>Cadastre uma conta financeira</h2>
                <p>
                  Crie uma conta bancaria, carteira ou conta de investimento
                  para organizar suas movimentacoes.
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
                  placeholder="Conta Principal"
                />
              </label>

              <div className={styles.fieldRow}>
                <label className={styles.field}>
                  <span>Tipo</span>
                  <select
                    value={formState.type}
                    onChange={(event) =>
                      handleChange(
                        "type",
                        event.target.value as FinancialAccountType,
                      )
                    }
                  >
                    <option value="bank_account">Conta bancaria</option>
                    <option value="wallet">Carteira</option>
                    <option value="investment_account">
                      Conta de investimento
                    </option>
                  </select>
                </label>

                <label className={styles.field}>
                  <span>Saldo inicial</span>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={formState.initialBalance}
                    onChange={(event) =>
                      handleChange(
                        "initialBalance",
                        Number(event.target.value || 0),
                      )
                    }
                  />
                </label>
              </div>

              <label className={styles.field}>
                <span>Instituicao</span>
                <input
                  value={formState.institutionName ?? ""}
                  onChange={(event) =>
                    handleChange("institutionName", event.target.value)
                  }
                  placeholder="Banco X"
                />
              </label>

              <label className={styles.field}>
                <span>Descricao</span>
                <textarea
                  value={formState.description ?? ""}
                  onChange={(event) =>
                    handleChange("description", event.target.value)
                  }
                  rows={4}
                  placeholder="Conta usada para movimentacoes principais"
                />
              </label>

              {submitError ? (
                <div className={styles.feedbackError}>{submitError}</div>
              ) : null}

              <div className={styles.formActions}>
                <button
                  className={styles.secondaryButton}
                  type="button"
                  onClick={handleCloseModal}
                  disabled={isSubmitting}
                >
                  Cancelar
                </button>

                <button
                  className={styles.primaryButton}
                  type="submit"
                  disabled={isSubmitting}
                >
                  {isSubmitting ? "Criando..." : "Criar conta"}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}
    </main>
  );
}


"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useAuth } from "@/features/auth/AuthProvider";
import { SharedSkeletonRows, SharedState } from "@/features/shared-state/SharedState";
import { ApiError } from "@/services/api-client";
import { getFinancialAccounts } from "@/services/financial-accounts-service";
import { getTransactionCategories } from "@/services/transaction-categories-service";
import { createExpenseTransaction, createIncomeTransaction, createTransferTransaction, getTransactions } from "@/services/transactions-service";
import type { FinancialAccount } from "@/types/financial-accounts";
import type { TransactionCategory } from "@/types/transaction-categories";
import type { CreateExpenseTransactionInput, CreateIncomeTransactionInput, CreateTransferTransactionInput, Transaction, TransactionType } from "@/types/transactions";
import styles from "./TransactionsPage.module.css";

type ModalMode = TransactionType | null;
type FiltersState = { from: string; to: string; type: "" | TransactionType; financialAccountId: string };

const TYPE_LABELS: Record<TransactionType, string> = { income: "Receita", expense: "Despesa", transfer: "Transferencia" };

function dateInput(date: Date) {
  const y = date.getFullYear();
  const m = `${date.getMonth() + 1}`.padStart(2, "0");
  const d = `${date.getDate()}`.padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function initialFilters(): FiltersState {
  const now = new Date();
  return { from: dateInput(new Date(now.getFullYear(), now.getMonth(), 1)), to: dateInput(now), type: "", financialAccountId: "" };
}

const incomeFormFactory = (): CreateIncomeTransactionInput => ({ financialAccountId: "", transactionCategoryId: "", amount: 0, occurredOn: dateInput(new Date()), description: "" });
const expenseFormFactory = (): CreateExpenseTransactionInput => ({ financialAccountId: "", transactionCategoryId: "", amount: 0, occurredOn: dateInput(new Date()), description: "" });
const transferFormFactory = (): CreateTransferTransactionInput => ({ sourceFinancialAccountId: "", destinationFinancialAccountId: "", amount: 0, occurredOn: dateInput(new Date()), description: "" });

const money = (value: number) => new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(value);
const displayDate = (value: string) => new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit", year: "numeric" }).format(new Date(`${value}T00:00:00`));

export function TransactionsPage() {
  const { logout, status, user } = useAuth();
  const [filters, setFilters] = useState<FiltersState>(initialFilters);
  const [accounts, setAccounts] = useState<FinancialAccount[]>([]);
  const [categories, setCategories] = useState<TransactionCategory[]>([]);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [modalMode, setModalMode] = useState<ModalMode>(null);
  const [incomeForm, setIncomeForm] = useState<CreateIncomeTransactionInput>(incomeFormFactory);
  const [expenseForm, setExpenseForm] = useState<CreateExpenseTransactionInput>(expenseFormFactory);
  const [transferForm, setTransferForm] = useState<CreateTransferTransactionInput>(transferFormFactory);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitSuccess, setSubmitSuccess] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const incomeCategories = useMemo(() => categories.filter((x) => x.type === "income"), [categories]);
  const expenseCategories = useMemo(() => categories.filter((x) => x.type === "expense"), [categories]);
  const accountNames = useMemo(() => new Map(accounts.map((x) => [x.id, x.name])), [accounts]);
  const categoryNames = useMemo(() => new Map(categories.map((x) => [x.id, x.name])), [categories]);
  const summary = useMemo(() => transactions.reduce((acc, t) => {
    if (t.type === "income") acc.income += t.amount;
    if (t.type === "expense") acc.expense += t.amount;
    if (t.type === "transfer") acc.transfer += t.amount;
    return acc;
  }, { income: 0, expense: 0, transfer: 0 }), [transactions]);

  const loadPageData = useCallback(async (next: FiltersState) => {
    setIsLoading(true); setLoadError(null);
    try {
      const [accountsData, categoriesData, transactionsData] = await Promise.all([
        getFinancialAccounts(),
        getTransactionCategories(),
        getTransactions({ from: next.from, to: next.to, type: next.type || undefined, financialAccountId: next.financialAccountId || undefined }),
      ]);
      setAccounts(accountsData); setCategories(categoriesData); setTransactions(transactionsData);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) { logout(); return; }
      setLoadError(error instanceof ApiError ? error.message : "Nao foi possivel carregar o modulo de transacoes agora.");
    } finally { setIsLoading(false); }
  }, [logout]);

  useEffect(() => {
    if (status !== "authenticated") return;
    const next = initialFilters();
    setFilters(next);
    void loadPageData(next);
  }, [status, loadPageData]);

  const hasAccounts = accounts.length > 0;
  const canCreateIncome = hasAccounts && incomeCategories.length > 0;
  const canCreateExpense = hasAccounts && expenseCategories.length > 0;
  const canCreateTransfer = accounts.length > 1;

  function openModal(mode: Exclude<ModalMode, null>) {
    setSubmitError(null); setSubmitSuccess(null);
    setIncomeForm(incomeFormFactory()); setExpenseForm(expenseFormFactory()); setTransferForm(transferFormFactory());
    setModalMode(mode);
  }

  function closeModal() {
    if (!isSubmitting) { setModalMode(null); setSubmitError(null); }
  }

  async function submitTransaction(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitError(null); setSubmitSuccess(null);
    try {
      if (modalMode === "income") {
        if (!incomeForm.financialAccountId || !incomeForm.transactionCategoryId || incomeForm.amount <= 0) { setSubmitError("Conta, categoria e valor valido sao obrigatorios para a receita."); return; }
        setIsSubmitting(true); await createIncomeTransaction({ ...incomeForm, description: incomeForm.description?.trim() || undefined }); setSubmitSuccess("Receita registrada com sucesso.");
      }
      if (modalMode === "expense") {
        if (!expenseForm.financialAccountId || !expenseForm.transactionCategoryId || expenseForm.amount <= 0) { setSubmitError("Conta, categoria e valor valido sao obrigatorios para a despesa."); return; }
        setIsSubmitting(true); await createExpenseTransaction({ ...expenseForm, description: expenseForm.description?.trim() || undefined }); setSubmitSuccess("Despesa registrada com sucesso.");
      }
      if (modalMode === "transfer") {
        if (!transferForm.sourceFinancialAccountId || !transferForm.destinationFinancialAccountId || transferForm.amount <= 0) { setSubmitError("Origem, destino e valor valido sao obrigatorios para a transferencia."); return; }
        if (transferForm.sourceFinancialAccountId === transferForm.destinationFinancialAccountId) { setSubmitError("A transferencia exige contas diferentes."); return; }
        setIsSubmitting(true); await createTransferTransaction({ ...transferForm, description: transferForm.description?.trim() || undefined }); setSubmitSuccess("Transferencia registrada com sucesso.");
      }
      setModalMode(null); await loadPageData(filters);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) { logout(); return; }
      setSubmitError(error instanceof ApiError ? error.message : "Nao foi possivel registrar a transacao agora.");
    } finally { setIsSubmitting(false); }
  }
  function detail(transaction: Transaction) {
    if (transaction.type === "transfer") {
      const from = transaction.sourceFinancialAccountId ? accountNames.get(transaction.sourceFinancialAccountId) : null;
      const to = transaction.destinationFinancialAccountId ? accountNames.get(transaction.destinationFinancialAccountId) : null;
      return { primary: `${from ?? "Conta de origem"} para ${to ?? "Conta de destino"}`, secondary: "Transferencia interna entre contas do usuario." };
    }

    const account = transaction.financialAccountId ? accountNames.get(transaction.financialAccountId) : null;
    const category = transaction.transactionCategoryId ? categoryNames.get(transaction.transactionCategoryId) : null;
    return { primary: account ?? "Conta nao encontrada", secondary: category ?? "Categoria nao encontrada" };
  }

  return (
    <main className={styles.main}>
      <header className={styles.header}>
        <div>
          <p className={styles.eyebrow}>Fase 2</p>
          <h1>Transacoes</h1>
          <p className={styles.subtitle}>Registre receitas, despesas e transferencias em cima das contas e categorias ja validadas da fase.</p>
        </div>
        <div className={styles.headerActions}>
          <div className={styles.userBadge}><span>Usuario autenticado</span><strong>{user?.fullName ?? "Sessao ativa"}</strong></div>
          <button className={styles.secondaryButton} onClick={logout}>Sair</button>
        </div>
      </header>

      <section className={styles.summaryGrid}>
        <article className={styles.summaryCard}><span>Total no periodo</span><strong>{transactions.length}</strong><small>Extrato operacional da Fase 2</small></article>
        <article className={styles.summaryCard}><span>Receitas</span><strong>{money(summary.income)}</strong><small>Entradas registradas</small></article>
        <article className={styles.summaryCard}><span>Despesas</span><strong>{money(summary.expense)}</strong><small>Saidas registradas</small></article>
        <article className={styles.summaryCard}><span>Transferencias</span><strong>{money(summary.transfer)}</strong><small>Movimentacao interna</small></article>
      </section>

      {submitSuccess ? <div className={styles.feedbackSuccess}>{submitSuccess}</div> : null}
      {!hasAccounts ? (
        <SharedState
          eyebrow="Base obrigatoria"
          title="Voce precisa de contas para iniciar o nucleo transacional"
          description="O modulo de transacoes depende de pelo menos uma conta financeira. Cadastre contas primeiro no modulo de Contas."
          tone="empty"
        />
      ) : null}
      {hasAccounts && categories.length === 0 ? (
        <SharedState
          eyebrow="Dependencia"
          title="Receitas e despesas exigem categorias"
          description="Transferencias ja podem ser registradas, mas receitas e despesas precisam de categorias compativeis."
          tone="warning"
          compact
        />
      ) : null}

      <section className={styles.filtersCard}>
        <form className={styles.filtersForm} onSubmit={async (event) => { event.preventDefault(); setSubmitSuccess(null); await loadPageData(filters); }}>
          <label className={styles.field}><span>De</span><input type="date" value={filters.from} onChange={(event) => setFilters((c) => ({ ...c, from: event.target.value }))} /></label>
          <label className={styles.field}><span>Ate</span><input type="date" value={filters.to} onChange={(event) => setFilters((c) => ({ ...c, to: event.target.value }))} /></label>
          <label className={styles.field}><span>Tipo</span><select value={filters.type} onChange={(event) => setFilters((c) => ({ ...c, type: event.target.value as FiltersState["type"] }))}><option value="">Todos</option><option value="income">Receita</option><option value="expense">Despesa</option><option value="transfer">Transferencia</option></select></label>
          <label className={styles.field}><span>Conta</span><select value={filters.financialAccountId} onChange={(event) => setFilters((c) => ({ ...c, financialAccountId: event.target.value }))}><option value="">Todas</option>{accounts.map((account) => <option key={account.id} value={account.id}>{account.name}</option>)}</select></label>
          <div className={styles.filterActions}><button className={styles.secondaryButton} type="button" onClick={() => void loadPageData(filters)}>Recarregar</button><button className={styles.primaryButton} type="submit">Aplicar filtros</button></div>
        </form>
      </section>

      <section className={styles.actionBar}>
        <button className={styles.primaryButton} onClick={() => openModal("income")} disabled={!canCreateIncome}>Nova receita</button>
        <button className={styles.primaryButton} onClick={() => openModal("expense")} disabled={!canCreateExpense}>Nova despesa</button>
        <button className={styles.primaryButton} onClick={() => openModal("transfer")} disabled={!canCreateTransfer}>Nova transferencia</button>
      </section>

      <section className={styles.listCard}>
        <div className={styles.listHeader}><div><h2>Extrato do periodo</h2><p>Visualize o nucleo operacional do dinheiro por data, tipo e conta.</p></div></div>
        {status === "loading" || isLoading ? <SharedSkeletonRows rows={3} /> : null}
        {status !== "loading" && !isLoading && loadError ? (
          <SharedState
            eyebrow="Extrato"
            title="Nao foi possivel carregar suas transacoes"
            description={loadError}
            tone="error"
            compact
            actions={<button className={styles.secondaryButton} onClick={() => void loadPageData(filters)}>Tentar novamente</button>}
          />
        ) : null}
        {status !== "loading" && !isLoading && !loadError && transactions.length === 0 ? (
          <SharedState
            eyebrow="Extrato"
            title="Nenhuma transacao encontrada no periodo"
            description="Ajuste os filtros ou registre sua primeira receita, despesa ou transferencia para iniciar o extrato operacional."
            tone="empty"
            compact
          />
        ) : null}
        {status !== "loading" && !isLoading && !loadError && transactions.length > 0 ? (
          <div className={styles.transactionList}>
            {transactions.map((transaction) => {
              const info = detail(transaction);
              return (
                <article className={styles.transactionCard} key={transaction.id}>
                  <div className={styles.transactionTopRow}>
                    <div><h3>{TYPE_LABELS[transaction.type]}</h3><p>{info.primary}</p><span className={styles.mutedText}>{info.secondary}</span></div>
                    <span className={`${styles.typeBadge} ${styles[`typeBadge${transaction.type}`]}`}>{TYPE_LABELS[transaction.type]}</span>
                  </div>
                  <div className={styles.transactionMetaRow}><div><span>Valor</span><strong>{money(transaction.amount)}</strong></div><div><span>Data</span><strong>{displayDate(transaction.occurredOn)}</strong></div><div><span>Status</span><strong>{transaction.status === "posted" ? "Realizada" : "Prevista"}</strong></div></div>
                  {transaction.description ? <p className={styles.transactionDescription}>{transaction.description}</p> : null}
                </article>
              );
            })}
          </div>
        ) : null}
      </section>
      {modalMode ? (
        <div className={styles.modalOverlay} role="presentation">
          <div className={styles.modalCard}>
            <div className={styles.modalHeader}>
              <div>
                <p className={styles.eyebrow}>Transactions Core</p>
                <h2>{modalMode === "income" ? "Registrar receita" : modalMode === "expense" ? "Registrar despesa" : "Registrar transferencia"}</h2>
                <p>{modalMode === "transfer" ? "Mova saldo entre duas contas do proprio usuario sem contaminar a leitura de receita e despesa." : "Registre uma movimentacao real vinculada a conta e categoria corretas do seu ambiente."}</p>
              </div>
              <button className={styles.iconButton} onClick={closeModal}>Fechar</button>
            </div>

            <form className={styles.form} onSubmit={submitTransaction}>
              {modalMode === "income" ? <>
                <div className={styles.fieldRow}>
                  <label className={styles.field}><span>Conta</span><select value={incomeForm.financialAccountId} onChange={(event) => setIncomeForm((c) => ({ ...c, financialAccountId: event.target.value }))}><option value="">Selecione</option>{accounts.map((account) => <option key={account.id} value={account.id}>{account.name}</option>)}</select></label>
                  <label className={styles.field}><span>Categoria</span><select value={incomeForm.transactionCategoryId} onChange={(event) => setIncomeForm((c) => ({ ...c, transactionCategoryId: event.target.value }))}><option value="">Selecione</option>{incomeCategories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label>
                </div>
                <div className={styles.fieldRow}>
                  <label className={styles.field}><span>Valor</span><input type="number" min="0" step="0.01" value={incomeForm.amount} onChange={(event) => setIncomeForm((c) => ({ ...c, amount: Number(event.target.value || 0) }))} /></label>
                  <label className={styles.field}><span>Data</span><input type="date" value={incomeForm.occurredOn} onChange={(event) => setIncomeForm((c) => ({ ...c, occurredOn: event.target.value }))} /></label>
                </div>
                <label className={styles.field}><span>Descricao</span><textarea rows={4} value={incomeForm.description ?? ""} onChange={(event) => setIncomeForm((c) => ({ ...c, description: event.target.value }))} placeholder="Recebimento principal do periodo" /></label>
              </> : null}

              {modalMode === "expense" ? <>
                <div className={styles.fieldRow}>
                  <label className={styles.field}><span>Conta</span><select value={expenseForm.financialAccountId} onChange={(event) => setExpenseForm((c) => ({ ...c, financialAccountId: event.target.value }))}><option value="">Selecione</option>{accounts.map((account) => <option key={account.id} value={account.id}>{account.name}</option>)}</select></label>
                  <label className={styles.field}><span>Categoria</span><select value={expenseForm.transactionCategoryId} onChange={(event) => setExpenseForm((c) => ({ ...c, transactionCategoryId: event.target.value }))}><option value="">Selecione</option>{expenseCategories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label>
                </div>
                <div className={styles.fieldRow}>
                  <label className={styles.field}><span>Valor</span><input type="number" min="0" step="0.01" value={expenseForm.amount} onChange={(event) => setExpenseForm((c) => ({ ...c, amount: Number(event.target.value || 0) }))} /></label>
                  <label className={styles.field}><span>Data</span><input type="date" value={expenseForm.occurredOn} onChange={(event) => setExpenseForm((c) => ({ ...c, occurredOn: event.target.value }))} /></label>
                </div>
                <label className={styles.field}><span>Descricao</span><textarea rows={4} value={expenseForm.description ?? ""} onChange={(event) => setExpenseForm((c) => ({ ...c, description: event.target.value }))} placeholder="Saida principal do periodo" /></label>
              </> : null}

              {modalMode === "transfer" ? <>
                <div className={styles.fieldRow}>
                  <label className={styles.field}><span>Conta de origem</span><select value={transferForm.sourceFinancialAccountId} onChange={(event) => setTransferForm((c) => ({ ...c, sourceFinancialAccountId: event.target.value }))}><option value="">Selecione</option>{accounts.map((account) => <option key={account.id} value={account.id}>{account.name}</option>)}</select></label>
                  <label className={styles.field}><span>Conta de destino</span><select value={transferForm.destinationFinancialAccountId} onChange={(event) => setTransferForm((c) => ({ ...c, destinationFinancialAccountId: event.target.value }))}><option value="">Selecione</option>{accounts.map((account) => <option key={account.id} value={account.id}>{account.name}</option>)}</select></label>
                </div>
                <div className={styles.fieldRow}>
                  <label className={styles.field}><span>Valor</span><input type="number" min="0" step="0.01" value={transferForm.amount} onChange={(event) => setTransferForm((c) => ({ ...c, amount: Number(event.target.value || 0) }))} /></label>
                  <label className={styles.field}><span>Data</span><input type="date" value={transferForm.occurredOn} onChange={(event) => setTransferForm((c) => ({ ...c, occurredOn: event.target.value }))} /></label>
                </div>
                <label className={styles.field}><span>Descricao</span><textarea rows={4} value={transferForm.description ?? ""} onChange={(event) => setTransferForm((c) => ({ ...c, description: event.target.value }))} placeholder="Movimentacao interna entre contas" /></label>
              </> : null}

              {submitError ? <div className={styles.feedbackError}>{submitError}</div> : null}
              <div className={styles.formActions}><button className={styles.secondaryButton} type="button" onClick={closeModal} disabled={isSubmitting}>Cancelar</button><button className={styles.primaryButton} type="submit" disabled={isSubmitting}>{isSubmitting ? "Salvando..." : "Registrar transacao"}</button></div>
            </form>
          </div>
        </div>
      ) : null}
    </main>
  );
}

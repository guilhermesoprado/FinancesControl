"use client";

import { useCallback, useEffect, useState } from "react";
import { useAuth } from "@/features/auth/AuthProvider";
import { ApiError } from "@/services/api-client";
import {
  createTransactionCategory,
  getTransactionCategories,
} from "@/services/transaction-categories-service";
import type {
  CreateTransactionCategoryInput,
  TransactionCategory,
  TransactionCategoryType,
} from "@/types/transaction-categories";
import styles from "./TransactionCategoriesPage.module.css";

const INITIAL_FORM: CreateTransactionCategoryInput = {
  name: "",
  type: "expense",
  color: "#22c55e",
  icon: "",
};

const TYPE_LABELS: Record<TransactionCategoryType, string> = {
  expense: "Despesa",
  income: "Receita",
};

function formatDate(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(new Date(value));
}

export function TransactionCategoriesPage() {
  const { logout, status, user } = useAuth();
  const [categories, setCategories] = useState<TransactionCategory[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [formState, setFormState] = useState<CreateTransactionCategoryInput>(INITIAL_FORM);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitSuccess, setSubmitSuccess] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadCategories = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);

    try {
      const data = await getTransactionCategories();
      setCategories(data);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      const message =
        error instanceof ApiError
          ? error.message
          : "Nao foi possivel carregar suas categorias agora.";

      setLoadError(message);
    } finally {
      setIsLoading(false);
    }
  }, [logout]);

  useEffect(() => {
    if (status !== "authenticated") {
      return;
    }

    void loadCategories();
  }, [status, loadCategories]);

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

  function handleChange<K extends keyof CreateTransactionCategoryInput>(
    field: K,
    value: CreateTransactionCategoryInput[K],
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
      setSubmitError("O nome da categoria e obrigatorio.");
      return;
    }

    setIsSubmitting(true);

    try {
      const created = await createTransactionCategory({
        ...formState,
        name: formState.name.trim(),
        color: formState.color?.trim() || undefined,
        icon: formState.icon?.trim() || undefined,
      });

      setCategories((current) =>
        [...current, created].sort((left, right) => {
          if (left.type === right.type) {
            return left.name.localeCompare(right.name);
          }

          return left.type.localeCompare(right.type);
        }),
      );
      setSubmitSuccess("Categoria criada com sucesso.");
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
          : "Nao foi possivel criar a categoria agora.";

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
          <h1>Categorias de transacao</h1>
          <p className={styles.subtitle}>
            Organize como suas futuras receitas e despesas serao classificadas no sistema.
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
            Nova categoria
          </button>
        </div>
      </header>

      <section className={styles.summaryGrid}>
        <article className={styles.summaryCard}>
          <span>Total de categorias</span>
          <strong>{categories.length}</strong>
          <small>Segundo modulo operacional da fase</small>
        </article>

        <article className={styles.summaryCard}>
          <span>Tipos suportados</span>
          <strong>2</strong>
          <small>Expense e income</small>
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
            <h2>Suas categorias</h2>
            <p>Visualize e crie as categorias que organizam suas receitas e despesas.</p>
          </div>

          <button className={styles.secondaryButton} onClick={() => void loadCategories()}>
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
            <h3>Nao foi possivel carregar suas categorias.</h3>
            <p>{loadError}</p>
            <button className={styles.secondaryButton} onClick={() => void loadCategories()}>
              Tentar novamente
            </button>
          </div>
        ) : null}

        {status !== "loading" && !isLoading && !loadError && categories.length === 0 ? (
          <div className={styles.stateBlock}>
            <h3>Nenhuma categoria cadastrada ainda</h3>
            <p>
              Voce ainda nao possui categorias cadastradas. Crie as categorias que irao organizar suas receitas e despesas.
            </p>
            <button className={styles.primaryButton} onClick={handleOpenModal}>
              Nova categoria
            </button>
          </div>
        ) : null}

        {status !== "loading" && !isLoading && !loadError && categories.length > 0 ? (
          <div className={styles.categoryList}>
            {categories.map((category) => (
              <article className={styles.categoryCard} key={category.id}>
                <div className={styles.categoryTopRow}>
                  <div className={styles.categoryIdentity}>
                    <span
                      className={styles.colorSwatch}
                      style={{ backgroundColor: category.color ?? "#1fcb90" }}
                    />
                    <div>
                      <h3>{category.name}</h3>
                      <p>{category.icon ?? "Sem icone definido"}</p>
                    </div>
                  </div>

                  <span className={styles.typeBadge}>{TYPE_LABELS[category.type]}</span>
                </div>

                <div className={styles.categoryMetaRow}>
                  <div>
                    <span>Status</span>
                    <strong>{category.isActive ? "Ativa" : "Inativa"}</strong>
                  </div>

                  <div>
                    <span>Origem</span>
                    <strong>{category.isSystem ? "Sistema" : "Usuario"}</strong>
                  </div>

                  <div>
                    <span>Criada em</span>
                    <strong>{formatDate(category.createdAtUtc)}</strong>
                  </div>
                </div>
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
                <p className={styles.eyebrow}>Nova categoria</p>
                <h2>Cadastre uma categoria transacional</h2>
                <p>
                  Crie categorias que organizarao suas receitas e despesas nos proximos modulos do sistema.
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
                  placeholder="Mercado"
                />
              </label>

              <div className={styles.fieldRow}>
                <label className={styles.field}>
                  <span>Tipo</span>
                  <select
                    value={formState.type}
                    onChange={(event) =>
                      handleChange("type", event.target.value as TransactionCategoryType)
                    }
                  >
                    <option value="expense">Despesa</option>
                    <option value="income">Receita</option>
                  </select>
                </label>

                <label className={styles.field}>
                  <span>Cor</span>
                  <input
                    type="color"
                    value={formState.color ?? "#22c55e"}
                    onChange={(event) => handleChange("color", event.target.value)}
                  />
                </label>
              </div>

              <label className={styles.field}>
                <span>Icone</span>
                <input
                  value={formState.icon ?? ""}
                  onChange={(event) => handleChange("icon", event.target.value)}
                  placeholder="shopping-cart"
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
                  {isSubmitting ? "Criando..." : "Criar categoria"}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}
    </main>
  );
}


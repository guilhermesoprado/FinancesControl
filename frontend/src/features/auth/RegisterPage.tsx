"use client";

import Link from "next/link";
import { useState } from "react";
import { ApiError } from "@/services/api-client";
import { useAuth } from "./AuthProvider";
import type { RegisterInput } from "./types";
import styles from "./AuthPage.module.css";

const INITIAL_FORM: RegisterInput = {
  fullName: "",
  email: "",
  password: "",
};

export function RegisterPage() {
  const { register, status } = useAuth();
  const [formState, setFormState] = useState<RegisterInput>(INITIAL_FORM);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitError(null);

    if (!formState.fullName.trim() || !formState.email.trim() || !formState.password.trim()) {
      setSubmitError("Preencha nome, e-mail e senha para criar a conta.");
      return;
    }

    setIsSubmitting(true);

    try {
      await register({
        fullName: formState.fullName.trim(),
        email: formState.email.trim(),
        password: formState.password,
      });
    } catch (error) {
      const message =
        error instanceof ApiError
          ? error.message
          : "Nao foi possivel concluir o cadastro agora.";

      setSubmitError(message);
      setIsSubmitting(false);
    }
  }

  return (
    <main className={styles.page}>
      <section className={styles.heroPanel}>
        <p className={styles.eyebrow}>Fase 2</p>
        <h1>Crie seu acesso e entre diretamente no modulo de contas.</h1>
        <p className={styles.heroText}>
          O cadastro do frontend ja nasce conectado aos endpoints reais de auth
          do backend, preparando o terreno para os proximos modulos do nucleo
          transacional.
        </p>
        <div className={styles.heroHighlights}>
          <div>
            <strong>Register real</strong>
            <span>POST /auth/register integrado</span>
          </div>
          <div>
            <strong>Entrada imediata</strong>
            <span>Cadastro autentica e redireciona para as contas</span>
          </div>
        </div>
      </section>

      <section className={styles.formPanel}>
        <div className={styles.formHeader}>
          <p className={styles.eyebrow}>Cadastro</p>
          <h2>Abra seu ambiente financeiro</h2>
          <p>
            Assim que o cadastro for concluido, o sistema cria a sessao e leva
            voce para Financial Accounts.
          </p>
        </div>

        <form className={styles.form} onSubmit={handleSubmit}>
          <label className={styles.field}>
            <span>Nome completo</span>
            <input
              value={formState.fullName}
              onChange={(event) =>
                setFormState((current) => ({
                  ...current,
                  fullName: event.target.value,
                }))
              }
              placeholder="Seu nome"
            />
          </label>

          <label className={styles.field}>
            <span>E-mail</span>
            <input
              type="email"
              value={formState.email}
              onChange={(event) =>
                setFormState((current) => ({
                  ...current,
                  email: event.target.value,
                }))
              }
              placeholder="voce@exemplo.com"
            />
          </label>

          <label className={styles.field}>
            <span>Senha</span>
            <input
              type="password"
              value={formState.password}
              onChange={(event) =>
                setFormState((current) => ({
                  ...current,
                  password: event.target.value,
                }))
              }
              placeholder="Crie uma senha"
            />
          </label>

          {submitError ? <div className={styles.feedbackError}>{submitError}</div> : null}

          <button className={styles.primaryButton} type="submit" disabled={isSubmitting || status === "loading"}>
            {isSubmitting ? "Criando acesso..." : "Criar conta"}
          </button>
        </form>

        <p className={styles.formFooter}>
          Ja possui acesso? <Link href="/login">Entrar</Link>
        </p>
      </section>
    </main>
  );
}

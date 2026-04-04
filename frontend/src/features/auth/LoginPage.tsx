"use client";

import Link from "next/link";
import { useState } from "react";
import { ApiError } from "@/services/api-client";
import { useAuth } from "./AuthProvider";
import type { LoginInput } from "./types";
import styles from "./AuthPage.module.css";

const INITIAL_FORM: LoginInput = {
  email: "",
  password: "",
};

export function LoginPage() {
  const { login, status } = useAuth();
  const [formState, setFormState] = useState<LoginInput>(INITIAL_FORM);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitError(null);

    if (!formState.email.trim() || !formState.password.trim()) {
      setSubmitError("Informe e-mail e senha para entrar.");
      return;
    }

    setIsSubmitting(true);

    try {
      await login({
        email: formState.email.trim(),
        password: formState.password,
      });
    } catch (error) {
      const message =
        error instanceof ApiError
          ? error.message
          : "Nao foi possivel concluir o login agora.";

      setSubmitError(message);
      setIsSubmitting(false);
    }
  }

  return (
    <main className={styles.page}>
      <section className={styles.heroPanel}>
        <p className={styles.eyebrow}>FinanceManager</p>
        <h1>A entrada do sistema agora leva para o nucleo financeiro real.</h1>
        <p className={styles.heroText}>
          A autenticacao do frontend passa a conversar com o backend real e leva
          o usuario diretamente para Financial Accounts, que e o primeiro modulo
          funcional da Fase 2.
        </p>
        <div className={styles.heroHighlights}>
          <div>
            <strong>JWT real</strong>
            <span>Bearer token validado com /auth/me</span>
          </div>
          <div>
            <strong>Rota protegida</strong>
            <span>Financial Accounts exige sessao autentica</span>
          </div>
        </div>
      </section>

      <section className={styles.formPanel}>
        <div className={styles.formHeader}>
          <p className={styles.eyebrow}>Login</p>
          <h2>Entre na sua area financeira</h2>
          <p>
            Use as credenciais da API para acessar o modulo de contas
            financeiras com o backend real.
          </p>
        </div>

        <form className={styles.form} onSubmit={handleSubmit}>
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
              placeholder="Sua senha"
            />
          </label>

          {submitError ? <div className={styles.feedbackError}>{submitError}</div> : null}

          <button className={styles.primaryButton} type="submit" disabled={isSubmitting || status === "loading"}>
            {isSubmitting ? "Entrando..." : "Entrar"}
          </button>
        </form>

        <p className={styles.formFooter}>
          Ainda nao possui conta? <Link href="/register">Criar cadastro</Link>
        </p>
      </section>
    </main>
  );
}

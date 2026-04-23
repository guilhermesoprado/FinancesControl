"use client";

import type { ReactNode } from "react";
import styles from "./SharedState.module.css";

type SharedStateTone = "neutral" | "loading" | "empty" | "error" | "warning";

type SharedStateProps = {
  title: string;
  description: string;
  tone?: SharedStateTone;
  eyebrow?: string;
  actions?: ReactNode;
  compact?: boolean;
};

const TONE_LABELS: Record<SharedStateTone, string> = {
  neutral: "Estado",
  loading: "Carregando",
  empty: "Sem resultados",
  error: "Falha",
  warning: "Atencao",
};

export function SharedState({
  title,
  description,
  tone = "neutral",
  eyebrow,
  actions,
  compact = false,
}: SharedStateProps) {
  return (
    <section
      className={`${styles.stateCard} ${styles[`tone${tone}`]} ${compact ? styles.compact : ""}`}
    >
      <div className={styles.header}>
        <span className={styles.pill}>{eyebrow ?? TONE_LABELS[tone]}</span>
        <span className={styles.accent} aria-hidden="true" />
      </div>
      <div className={styles.body}>
        <h3>{title}</h3>
        <p>{description}</p>
      </div>
      {actions ? <div className={styles.actions}>{actions}</div> : null}
    </section>
  );
}

type SharedSkeletonRowsProps = {
  rows?: number;
};

export function SharedSkeletonRows({ rows = 3 }: SharedSkeletonRowsProps) {
  return (
    <div className={styles.skeletonList} aria-hidden="true">
      {Array.from({ length: rows }).map((_, index) => (
        <div className={styles.skeletonRow} key={index} />
      ))}
    </div>
  );
}

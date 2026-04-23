"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { SharedSkeletonRows, SharedState } from "@/features/shared-state/SharedState";
import { ApiError } from "@/services/api-client";
import { getAuditLogs } from "@/services/audit-logs-service";
import { useAuth } from "@/features/auth/AuthProvider";
import type {
  AuditLog,
  AuditLogAction,
  AuditLogEntityType,
} from "@/types/audit-logs";
import styles from "./SettingsGovernancePage.module.css";

const ENTITY_LABELS: Record<AuditLogEntityType, string> = {
  financial_account: "Conta financeira",
  transaction_category: "Categoria",
};

const ACTION_LABELS: Record<AuditLogAction, string> = {
  created: "Criado",
  updated: "Atualizado",
  inactivated: "Inativado",
};

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
}

export function SettingsGovernancePage() {
  const { logout, status } = useAuth();
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [entityType, setEntityType] = useState<AuditLogEntityType | "">("");
  const [action, setAction] = useState<AuditLogAction | "">("");
  const [entityId, setEntityId] = useState("");
  const [search, setSearch] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [limit, setLimit] = useState("100");

  const loadLogs = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);

    try {
      const data = await getAuditLogs({
        entityType: entityType || undefined,
        action: action || undefined,
        entityId: entityId.trim() || undefined,
        search: search.trim() || undefined,
        from: from || undefined,
        to: to || undefined,
        limit: Number(limit),
      });
      setLogs(data);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        return;
      }

      setLoadError(
        error instanceof ApiError
          ? error.message
          : "Nao foi possivel carregar a trilha de auditoria agora.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [action, entityId, entityType, from, limit, logout, search, to]);

  useEffect(() => {
    if (status !== "authenticated") {
      return;
    }

    void loadLogs();
  }, [loadLogs, status]);

  const summary = useMemo(() => {
    const created = logs.filter((item) => item.action === "created").length;
    const updated = logs.filter((item) => item.action === "updated").length;
    const inactivated = logs.filter((item) => item.action === "inactivated").length;

    return { created, updated, inactivated };
  }, [logs]);

  function handleClearFilters() {
    setEntityType("");
    setAction("");
    setEntityId("");
    setSearch("");
    setFrom("");
    setTo("");
    setLimit("100");
  }

  return (
    <main className={styles.main}>
      <header className={styles.header}>
        <div>
          <p className={styles.eyebrow}>Fase 5</p>
          <h1>Governanca e rastreabilidade</h1>
          <p className={styles.subtitle}>
            Leia a trilha basica das mutacoes governadas do sistema sem abrir um
            modulo pesado de auditoria. Aqui ficam visiveis os eventos de
            criacao, edicao e inativacao de contas e categorias.
          </p>
        </div>

        <div className={styles.headerActions}>
          <button className={styles.secondaryButton} onClick={logout}>
            Sair
          </button>
          <button className={styles.primaryButton} onClick={() => void loadLogs()}>
            Recarregar auditoria
          </button>
        </div>
      </header>

      <section className={styles.summaryGrid}>
        <article className={`${styles.summaryCard} ${styles.summaryLeadCard}`}>
          <span>Eventos visiveis</span>
          <strong>{logs.length}</strong>
          <small>Leitura atual do recorte filtrado</small>
        </article>

        <article className={styles.summaryCard}>
          <span>Criacoes e edicoes</span>
          <strong>{summary.created + summary.updated}</strong>
          <small>{summary.created} criacoes e {summary.updated} edicoes</small>
        </article>

        <article className={styles.summaryCard}>
          <span>Inativacoes</span>
          <strong>{summary.inactivated}</strong>
          <small>Eventos que retiraram entidades de operacao</small>
        </article>
      </section>

      <section className={styles.panel}>
        <div className={styles.panelIntro}>
          <h2>Filtros de leitura</h2>
          <p className={styles.subtitle}>
            Filtre por tipo de entidade, acao e periodo para localizar a mutacao
            que voce quer auditar.
          </p>
        </div>

        <div className={styles.filterSection}>
          <div className={styles.sectionLabel}>Recorte principal</div>
          <div className={styles.filtersRow}>
            <label className={styles.field}>
              <span>Entidade</span>
              <select
                value={entityType}
                onChange={(event) => setEntityType(event.target.value as AuditLogEntityType | "")}
              >
                <option value="">Todas</option>
                <option value="financial_account">Contas financeiras</option>
                <option value="transaction_category">Categorias</option>
              </select>
            </label>

            <label className={styles.field}>
              <span>Acao</span>
              <select
                value={action}
                onChange={(event) => setAction(event.target.value as AuditLogAction | "")}
              >
                <option value="">Todas</option>
                <option value="created">Criado</option>
                <option value="updated">Atualizado</option>
                <option value="inactivated">Inativado</option>
              </select>
            </label>

            <label className={styles.field}>
              <span>De</span>
              <input type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
            </label>

            <label className={styles.field}>
              <span>Ate</span>
              <input type="date" value={to} onChange={(event) => setTo(event.target.value)} />
            </label>
          </div>
        </div>

        <div className={styles.filterSection}>
          <div className={styles.sectionLabel}>Busca e detalhamento</div>
          <div className={styles.filtersRowSecondary}>
            <label className={styles.field}>
              <span>Busca textual</span>
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Ex.: principal, categoria, inativada"
              />
            </label>

            <label className={styles.field}>
              <span>Entity ID</span>
              <input
                value={entityId}
                onChange={(event) => setEntityId(event.target.value)}
                placeholder="GUID da entidade auditada"
              />
            </label>

            <label className={styles.field}>
              <span>Limite</span>
              <select value={limit} onChange={(event) => setLimit(event.target.value)}>
                <option value="25">25</option>
                <option value="50">50</option>
                <option value="100">100</option>
                <option value="200">200</option>
              </select>
            </label>
          </div>
        </div>

        <div className={styles.panelActions}>
          <button className={styles.secondaryButton} onClick={handleClearFilters}>
            Limpar filtros
          </button>
          <button className={styles.secondaryButton} onClick={() => void loadLogs()}>
            Aplicar filtros
          </button>
          <button className={styles.primaryButton} onClick={() => void loadLogs()}>
            Consultar auditoria
          </button>
        </div>

        {loadError ? <div className={styles.feedbackError}>{loadError}</div> : null}
      </section>

      <section className={styles.logsSection}>
        <div className={styles.sectionHeading}>
          <div>
            <h2>Eventos rastreados</h2>
            <p className={styles.subtitle}>
              A leitura abaixo preserva o recorte atual e organiza os eventos mais recentes em ordem cronologica.
            </p>
          </div>
        </div>

        {status === "loading" || isLoading ? (
          <SharedSkeletonRows rows={4} />
        ) : null}

        {status !== "loading" && !isLoading && !loadError && logs.length === 0 ? (
          <SharedState
            tone="empty"
            eyebrow="Sem eventos"
            title="Nenhum evento encontrado"
            description="Nao ha eventos de auditoria para o recorte atual. Ajuste os filtros ou gere novas mutacoes governadas no sistema."
            actions={
              <button className={styles.secondaryButton} onClick={handleClearFilters}>
                Limpar filtros
              </button>
            }
          />
        ) : null}

        {status !== "loading" && !isLoading && logs.length > 0 ? (
          <section className={styles.logsList}>
            {logs.map((log) => (
              <article className={styles.logCard} key={log.id}>
                <div className={styles.logTopRow}>
                  <div className={styles.badgeRow}>
                    <span className={styles.badge}>{ENTITY_LABELS[log.entityType]}</span>
                    <span className={styles.badge}>{ACTION_LABELS[log.action]}</span>
                  </div>
                  <span className={styles.logMeta}>{formatDateTime(log.createdAtUtc)}</span>
                </div>

                <p className={styles.logSummary}>{log.summary}</p>
                <span className={styles.logMeta}>Entidade afetada: {log.entityId}</span>
              </article>
            ))}
          </section>
        ) : null}
      </section>
    </main>
  );
}

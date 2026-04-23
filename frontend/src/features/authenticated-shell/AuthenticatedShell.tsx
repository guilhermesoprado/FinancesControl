"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import styles from "./AuthenticatedShell.module.css";

type AuthenticatedShellProps = {
  children: React.ReactNode;
};

type NavItem = {
  label: string;
  href?: string;
};

const NAV_ITEMS: NavItem[] = [
  { label: "Dashboard", href: "/dashboard" },
  { label: "Contas", href: "/financial-accounts" },
  { label: "Categorias", href: "/transaction-categories" },
  { label: "Transacoes", href: "/transactions" },
  { label: "Cartoes", href: "/credit-cards" },
  { label: "Faturas", href: "/invoices" },
  { label: "Planejamento", href: "/scheduled-entries" },
  { label: "Settings", href: "/settings" },
];

function isActive(pathname: string, href: string) {
  return pathname === href || pathname.startsWith(`${href}/`);
}

export function AuthenticatedShell({ children }: AuthenticatedShellProps) {
  const pathname = usePathname();
  const activeItem = NAV_ITEMS.find((item) => item.href && isActive(pathname, item.href));

  return (
    <div className={styles.shell}>
      <aside className={styles.sidebar}>
        <div className={styles.sidebarInner}>
          <div className={styles.brandBlock}>
            <div className={styles.brand}>
              <div className={styles.brandIcon}>F</div>
              <div>
                <p className={styles.brandTitle}>FinanceManager</p>
                <p className={styles.brandSubtitle}>Premium Wealth</p>
              </div>
            </div>

            <div className={styles.brandPanel}>
              <span className={styles.brandPanelEyebrow}>Workspace</span>
              <strong>{activeItem?.label ?? "Ambiente autenticado"}</strong>
              <p>Navegacao central do produto com foco em leitura clara e operacao segura.</p>
            </div>
          </div>

          <div className={styles.navSection}>
            <p className={styles.navCaption}>Modulos</p>
            <nav className={styles.nav} aria-label="Navegacao principal">
              {NAV_ITEMS.map((item) => {
                if (!item.href) {
                  return (
                    <span
                      key={item.label}
                      className={`${styles.navItem} ${styles.navItemDisabled}`}
                      aria-disabled="true"
                    >
                      <span className={styles.navItemDot} aria-hidden="true" />
                      {item.label}
                    </span>
                  );
                }

                const active = isActive(pathname, item.href);

                return (
                  <Link
                    key={item.href}
                    className={`${styles.navItem} ${active ? styles.navItemActive : ""}`}
                    href={item.href}
                    scroll={false}
                  >
                    <span className={styles.navItemDot} aria-hidden="true" />
                    {item.label}
                  </Link>
                );
              })}
            </nav>
          </div>

          <div className={styles.sidebarFooter}>
            <span className={styles.sidebarFooterLabel}>Estado atual</span>
            <strong>{activeItem?.label ?? "Painel principal"}</strong>
            <p>Fluxos preservados, linguagem visual em consolidacao.</p>
          </div>
        </div>
      </aside>

      <div className={styles.content}>
        <div className={styles.contentInner}>{children}</div>
      </div>
    </div>
  );
}

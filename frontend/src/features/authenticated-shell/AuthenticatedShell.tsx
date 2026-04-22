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
];

function isActive(pathname: string, href: string) {
  return pathname === href || pathname.startsWith(`${href}/`);
}

export function AuthenticatedShell({ children }: AuthenticatedShellProps) {
  const pathname = usePathname();

  return (
    <div className={styles.shell}>
      <aside className={styles.sidebar}>
        <div className={styles.brand}>
          <div className={styles.brandIcon}>F</div>
          <div>
            <p className={styles.brandTitle}>FinanceManager</p>
            <p className={styles.brandSubtitle}>Premium Wealth</p>
          </div>
        </div>

        <nav className={styles.nav} aria-label="Navegacao principal">
          {NAV_ITEMS.map((item) => {
            if (!item.href) {
              return (
                <span
                  key={item.label}
                  className={`${styles.navItem} ${styles.navItemDisabled}`}
                  aria-disabled="true"
                >
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
                {item.label}
              </Link>
            );
          })}
        </nav>
      </aside>

      <div className={styles.content}>{children}</div>
    </div>
  );
}

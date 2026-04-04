"use client";

import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { usePathname, useRouter } from "next/navigation";
import {
  getCurrentUser,
  login as loginRequest,
  register as registerRequest,
} from "@/services/auth-service";
import {
  clearAuthSession,
  persistAuthSession,
  readStoredAuthSession,
} from "./auth-session";
import type {
  AuthSession,
  AuthenticatedUser,
  LoginInput,
  RegisterInput,
} from "./types";

type AuthStatus = "loading" | "authenticated" | "unauthenticated";

interface AuthContextValue {
  status: AuthStatus;
  session: AuthSession | null;
  user: AuthenticatedUser | null;
  login: (input: LoginInput) => Promise<void>;
  register: (input: RegisterInput) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const PUBLIC_PATHS = new Set(["/login", "/register"]);
const AUTHENTICATED_HOME_PATH = "/financial-accounts";

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [session, setSession] = useState<AuthSession | null>(null);
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    let isMounted = true;

    async function hydrateSession() {
      const storedSession = readStoredAuthSession();

      if (!storedSession) {
        if (isMounted) {
          setSession(null);
          setStatus("unauthenticated");
        }

        return;
      }

      try {
        const user = await getCurrentUser();
        const nextSession = {
          ...storedSession,
          user,
        };

        persistAuthSession(nextSession);

        if (isMounted) {
          setSession(nextSession);
          setStatus("authenticated");
        }
      } catch {
        clearAuthSession();

        if (isMounted) {
          setSession(null);
          setStatus("unauthenticated");

          if (!PUBLIC_PATHS.has(pathname)) {
            router.replace("/login");
          }
        }
      }
    }

    void hydrateSession();

    return () => {
      isMounted = false;
    };
  }, [pathname, router]);

  async function handleLogin(input: LoginInput) {
    const nextSession = await loginRequest(input);
    persistAuthSession(nextSession);
    setSession(nextSession);
    setStatus("authenticated");
    router.replace(AUTHENTICATED_HOME_PATH);
    router.refresh();
  }

  async function handleRegister(input: RegisterInput) {
    const nextSession = await registerRequest(input);
    persistAuthSession(nextSession);
    setSession(nextSession);
    setStatus("authenticated");
    router.replace(AUTHENTICATED_HOME_PATH);
    router.refresh();
  }

  function handleLogout() {
    clearAuthSession();
    setSession(null);
    setStatus("unauthenticated");
    router.replace("/login");
    router.refresh();
  }

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      session,
      user: session?.user ?? null,
      login: handleLogin,
      register: handleRegister,
      logout: handleLogout,
    }),
    [session, status],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used within AuthProvider.");
  }

  return context;
}

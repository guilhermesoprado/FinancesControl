import { AUTH_SESSION_STORAGE_KEY, AUTH_TOKEN_COOKIE_NAME } from "./auth-constants";
import type { AuthSession } from "./types";

function isBrowser() {
  return typeof window !== "undefined";
}

function hasSessionExpired(expiresAtUtc: string) {
  const expiresAt = new Date(expiresAtUtc).getTime();

  if (Number.isNaN(expiresAt)) {
    return true;
  }

  return expiresAt <= Date.now();
}

function readCookie(name: string) {
  if (!isBrowser()) {
    return null;
  }

  const cookie = document.cookie
    .split("; ")
    .find((entry) => entry.startsWith(`${name}=`));

  return cookie ? decodeURIComponent(cookie.split("=")[1] ?? "") : null;
}

function writeTokenCookie(accessToken: string, expiresAtUtc: string) {
  if (!isBrowser()) {
    return;
  }

  const expiresAt = new Date(expiresAtUtc);
  const expiresValue = Number.isNaN(expiresAt.getTime())
    ? ""
    : `; expires=${expiresAt.toUTCString()}`;

  document.cookie =
    `${AUTH_TOKEN_COOKIE_NAME}=${encodeURIComponent(accessToken)}; path=/; SameSite=Lax${expiresValue}`;
}

function clearTokenCookie() {
  if (!isBrowser()) {
    return;
  }

  document.cookie =
    `${AUTH_TOKEN_COOKIE_NAME}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT; SameSite=Lax`;
}

export function persistAuthSession(session: AuthSession) {
  if (!isBrowser()) {
    return;
  }

  window.localStorage.setItem(AUTH_SESSION_STORAGE_KEY, JSON.stringify(session));
  writeTokenCookie(session.accessToken, session.expiresAtUtc);
}

export function clearAuthSession() {
  if (!isBrowser()) {
    return;
  }

  window.localStorage.removeItem(AUTH_SESSION_STORAGE_KEY);
  clearTokenCookie();
}

export function readStoredAuthSession(): AuthSession | null {
  if (!isBrowser()) {
    return null;
  }

  const raw = window.localStorage.getItem(AUTH_SESSION_STORAGE_KEY);

  if (!raw) {
    return null;
  }

  try {
    const session = JSON.parse(raw) as AuthSession;

    if (!session.accessToken || !session.expiresAtUtc || !session.user) {
      clearAuthSession();
      return null;
    }

    if (hasSessionExpired(session.expiresAtUtc)) {
      clearAuthSession();
      return null;
    }

    return session;
  } catch {
    clearAuthSession();
    return null;
  }
}

export function getAccessToken() {
  const storedSession = readStoredAuthSession();

  if (storedSession?.accessToken) {
    return storedSession.accessToken;
  }

  return readCookie(AUTH_TOKEN_COOKIE_NAME) || null;
}

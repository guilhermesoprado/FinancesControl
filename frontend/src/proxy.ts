import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { AUTH_TOKEN_COOKIE_NAME } from "@/features/auth/auth-constants";

const PUBLIC_PATHS = new Set(["/login", "/register"]);
const AUTHENTICATED_PATHS = ["/financial-accounts", "/transaction-categories", "/transactions"];

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const accessToken = request.cookies.get(AUTH_TOKEN_COOKIE_NAME)?.value;
  const isAuthenticated = Boolean(accessToken);

  if (pathname === "/") {
    const target = isAuthenticated ? "/financial-accounts" : "/login";
    return NextResponse.redirect(new URL(target, request.url));
  }

  if (PUBLIC_PATHS.has(pathname) && isAuthenticated) {
    return NextResponse.redirect(new URL("/financial-accounts", request.url));
  }

  if (AUTHENTICATED_PATHS.some((path) => pathname.startsWith(path)) && !isAuthenticated) {
    return NextResponse.redirect(new URL("/login", request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/", "/login", "/register", "/financial-accounts/:path*", "/transaction-categories/:path*", "/transactions/:path*"],
};
import { apiRequest } from "./api-client";
import type {
  AuthenticatedUser,
  AuthSession,
  LoginInput,
  RegisterInput,
} from "@/features/auth/types";

export function login(input: LoginInput): Promise<AuthSession> {
  return apiRequest<AuthSession>("/auth/login", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function register(input: RegisterInput): Promise<AuthSession> {
  return apiRequest<AuthSession>("/auth/register", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function getCurrentUser(): Promise<AuthenticatedUser> {
  return apiRequest<AuthenticatedUser>("/auth/me");
}

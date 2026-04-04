export interface AuthenticatedUser {
  id: string;
  fullName: string;
  email: string;
}

export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
  user: AuthenticatedUser;
}

export interface LoginInput {
  email: string;
  password: string;
}

export interface RegisterInput {
  fullName: string;
  email: string;
  password: string;
}

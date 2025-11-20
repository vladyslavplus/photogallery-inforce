export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  userName: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
}

export interface DecodedToken {
  sub: string;
  unique_name: string;
  email: string;
  role: string | string[];
  exp: number;
  iat: number;
}

export interface RefreshConfig {
  maxRetries: number;
  baseDelayMs: number;
  refreshThresholdSeconds: number;
}
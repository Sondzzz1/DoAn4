/**
 * Register Request
 */
export interface RegisterRequest {
  fullName: string;
  email: string;
  phone: string;
  password: string;
  confirmPassword?: string;
  roleName?: 'Tenant' | 'Landlord';
}

/**
 * Login Request
 */
export interface LoginRequest {
  email: string;
  password: string;
}

/**
 * Auth Response
 */
export interface AuthResponse {
  userId: number;
  fullName: string;
  email: string;
  role: string;
  token: string;
  expiresAt: string;
}

/**
 * Current User (stored in context)
 */
export interface User {
  userId: number;
  id?: number;
  fullName: string;
  email: string;
  role: string;
}

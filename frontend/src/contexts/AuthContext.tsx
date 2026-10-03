import React, { ReactNode, useState } from 'react';
import { User, AuthResponse, LoginRequest, RegisterRequest } from '../types/auth.types';
import { authService } from '../services/authService';
import { STORAGE_KEYS } from '../utils/constants';
import { AuthContext, AuthContextType } from './AuthContextDefinition';

interface AuthProviderProps {
  children: ReactNode;
}

const getStoredUser = (): User | null => {
  const storedUser = localStorage.getItem(STORAGE_KEYS.USER);
  if (!storedUser) return null;

  try {
    return JSON.parse(storedUser) as User;
  } catch {
    localStorage.removeItem(STORAGE_KEYS.USER);
    localStorage.removeItem(STORAGE_KEYS.TOKEN);
    return null;
  }
};

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(getStoredUser);
  const [token, setToken] = useState<string | null>(() => (
    getStoredUser() ? localStorage.getItem(STORAGE_KEYS.TOKEN) : null
  ));
  const [isLoading] = useState(false);

  /**
   * Login
   */
  const login = async (data: LoginRequest) => {
    const response = await authService.login(data);
    const authData: AuthResponse = response.data;

    // Lưu token
    localStorage.setItem(STORAGE_KEYS.TOKEN, authData.token);

    // Lưu user info
    const userData: User = {
      userId: authData.userId,
      id: authData.userId,
      fullName: authData.fullName,
      email: authData.email,
      role: authData.role,
    };
    localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(userData));

    setToken(authData.token);
    setUser(userData);
  };

  /**
   * Register
   */
  const register = async (data: RegisterRequest) => {
    const response = await authService.register(data);
    const authData: AuthResponse = response.data;

    // Lưu token
    localStorage.setItem(STORAGE_KEYS.TOKEN, authData.token);

    // Lưu user info
    const userData: User = {
      userId: authData.userId,
      id: authData.userId,
      fullName: authData.fullName,
      email: authData.email,
      role: authData.role,
    };
    localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(userData));

    setToken(authData.token);
    setUser(userData);
  };

  /**
   * Logout
   */
  const logout = () => {
    localStorage.removeItem(STORAGE_KEYS.TOKEN);
    localStorage.removeItem(STORAGE_KEYS.USER);
    setToken(null);
    setUser(null);
  };

  const value: AuthContextType = {
    user,
    token,
    isLoading,
    login,
    register,
    logout,
    isAuthenticated: !!token && !!user,
    isLandlord: user?.role === 'Landlord',
    isTenant: user?.role === 'Tenant',
    isAdmin: user?.role === 'Admin',
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

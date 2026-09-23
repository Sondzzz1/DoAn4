import React, { createContext, useState, useEffect, ReactNode } from 'react';
import { User, AuthResponse, LoginRequest, RegisterRequest } from '../types/auth.types';
import { authService } from '../services/authService';
import { STORAGE_KEYS } from '../utils/constants';

interface AuthContextType {
  user: User | null;
  token: string | null;
  isLoading: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
  isLandlord: boolean;
  isTenant: boolean;
  isAdmin: boolean;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Load user từ localStorage khi app khởi động
  useEffect(() => {
    const storedToken = localStorage.getItem(STORAGE_KEYS.TOKEN);
    const storedUser = localStorage.getItem(STORAGE_KEYS.USER);

    if (storedToken && storedUser) {
      setToken(storedToken);
      setUser(JSON.parse(storedUser));
    }

    setIsLoading(false);
  }, []);

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

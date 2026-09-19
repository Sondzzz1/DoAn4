import api from './api';
import { ApiResponse } from '../types/common.types';
import { RegisterRequest, LoginRequest, AuthResponse } from '../types/auth.types';

/**
 * Auth Service
 */
export const authService = {
  /**
   * Đăng ký
   */
  register: async (data: RegisterRequest): Promise<ApiResponse<AuthResponse>> => {
    const response = await api.post<ApiResponse<AuthResponse>>('/xac-thuc/dang-ky', {
      hoTen: data.fullName,
      email: data.email,
      soDienThoai: data.phone,
      matKhau: data.password,
      xacNhanMatKhau: data.confirmPassword || data.password,
      vaiTro: data.roleName,
    });
    return response.data;
  },

  /**
   * Đăng nhập
   */
  login: async (data: LoginRequest): Promise<ApiResponse<AuthResponse>> => {
    const response = await api.post<ApiResponse<AuthResponse>>('/xac-thuc/dang-nhap', {
      email: data.email,
      matKhau: data.password,
    });
    return response.data;
  },
};

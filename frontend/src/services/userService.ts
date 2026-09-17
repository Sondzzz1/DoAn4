import api from './api';
import { ApiResponse } from '../types/common.types';
import { UserProfile, UpdateProfileRequest, ChangePasswordRequest } from '../types/user.types';

/**
 * User Service
 */
export const userService = {
  /**
   * Lấy thông tin profile
   */
  getProfile: async (): Promise<ApiResponse<UserProfile>> => {
    const response = await api.get<ApiResponse<UserProfile>>('/nguoi-dung/thong-tin');
    return response.data;
  },

  /**
   * Cập nhật profile
   */
  updateProfile: async (data: UpdateProfileRequest): Promise<ApiResponse<UserProfile>> => {
    const response = await api.put<ApiResponse<UserProfile>>('/nguoi-dung/thong-tin', data);
    return response.data;
  },

  /**
   * Đổi mật khẩu
   */
  changePassword: async (data: ChangePasswordRequest): Promise<ApiResponse<null>> => {
    const response = await api.post<ApiResponse<null>>('/nguoi-dung/doi-mat-khau', data);
    return response.data;
  },
};

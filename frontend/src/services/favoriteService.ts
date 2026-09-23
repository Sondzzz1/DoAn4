import api from './api';
import { ApiResponse } from '../types/common.types';
import { PostListItem } from '../types/post.types';

export const favoriteService = {
  /**
   * Lấy danh sách phòng đã yêu thích của người dùng hiện tại
   */
  getMyFavorites: async (): Promise<ApiResponse<PostListItem[]>> => {
    const response = await api.get<ApiResponse<PostListItem[]>>('/yeu-thich/cua-toi');
    return response.data;
  },

  /**
   * Thêm phòng vào danh sách yêu thích
   */
  addFavorite: async (postId: number): Promise<ApiResponse<boolean>> => {
    const response = await api.post<ApiResponse<boolean>>('/yeu-thich', { postId });
    return response.data;
  },

  /**
   * Xóa phòng khỏi danh sách yêu thích
   */
  removeFavorite: async (postId: number): Promise<ApiResponse<boolean>> => {
    const response = await api.delete<ApiResponse<boolean>>(`/yeu-thich/${postId}`);
    return response.data;
  },

  /**
   * Kiểm tra bài đăng đã được yêu thích chưa
   */
  checkFavorite: async (postId: number): Promise<ApiResponse<boolean>> => {
    const response = await api.get<ApiResponse<boolean>>(`/yeu-thich/kiem-tra/${postId}`);
    return response.data;
  },
};

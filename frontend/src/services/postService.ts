import api from './api';
import { ApiResponse } from '../types/common.types';
import { Post, PostListItem, CreatePostRequest, UpdatePostRequest, PostSearchParams } from '../types/post.types';

/**
 * Post Service
 */
export const postService = {
  /**
   * Tạo tin đăng mới (Landlord)
   */
  createPost: async (data: CreatePostRequest): Promise<ApiResponse<Post>> => {
    const response = await api.post<ApiResponse<Post>>('/bai-dang', data);
    return response.data;
  },

  /**
   * Lấy danh sách tin của Landlord
   */
  getMyPosts: async (): Promise<ApiResponse<PostListItem[]>> => {
    const response = await api.get<ApiResponse<PostListItem[]>>('/bai-dang/cua-toi');
    return response.data;
  },

  /**
   * Lấy chi tiết tin đăng
   */
  getPostById: async (id: number): Promise<ApiResponse<Post>> => {
    const response = await api.get<ApiResponse<Post>>(`/bai-dang/${id}`);
    return response.data;
  },

  /**
   * Cập nhật tin đăng
   */
  updatePost: async (id: number, data: UpdatePostRequest): Promise<ApiResponse<Post>> => {
    const response = await api.put<ApiResponse<Post>>(`/bai-dang/${id}`, data);
    return response.data;
  },

  /**
   * Xóa tin đăng
   */
  deletePost: async (id: number): Promise<ApiResponse<null>> => {
    const response = await api.delete<ApiResponse<null>>(`/bai-dang/${id}`);
    return response.data;
  },

  /**
   * Cập nhật trạng thái tin đăng
   */
  updatePostStatus: async (id: number, status: number): Promise<ApiResponse<Post>> => {
    const response = await api.put<ApiResponse<Post>>(`/bai-dang/${id}/trang-thai`, { status });
    return response.data;
  },

  /**
   * Tìm kiếm phòng công khai
   */
  searchPosts: async (params?: PostSearchParams): Promise<ApiResponse<PostListItem[]>> => {
    const response = await api.get<ApiResponse<PostListItem[]>>('/bai-dang', { params });
    return response.data;
  },

  /**
   * Lấy danh sách tất cả tin đăng (alias for searchPosts)
   */
  getPosts: async (params?: PostSearchParams): Promise<ApiResponse<PostListItem[]>> => {
    return postService.searchPosts(params);
  },
};

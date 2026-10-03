import api from './api';
import { ApiResponse } from '../types/common.types';
import { BlogPost } from '../types/blog.types';

export const blogService = {
  getPublishedPosts: async (): Promise<ApiResponse<BlogPost[]>> =>
    (await api.get<ApiResponse<BlogPost[]>>('/bai-viet')).data,
  getPostById: async (id: number): Promise<ApiResponse<BlogPost>> =>
    (await api.get<ApiResponse<BlogPost>>(`/bai-viet/${id}`)).data,
};

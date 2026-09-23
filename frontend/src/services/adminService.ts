import api from './api';
import { ApiResponse } from '../types/common.types';
import { PostListItem } from '../types/post.types';

export interface AdminUser {
  id: number;
  fullName: string;
  email: string;
  phone?: string | null;
  roleId: number;
  roleName: string;
  isActive: boolean;
  createdAt: string;
  postCount: number;
  roomCount: number;
}

export interface AdminRoom {
  id: number;
  roomName: string;
  landlordName: string;
  categoryName: string;
  price: number;
  area: number;
  status: number;
  statusText?: string;
  address: string;
}

export interface AdminCatalogItem {
  id: number;
  name: string;
  description?: string | null;
  icon?: string | null;
  isActive?: boolean;
}

export interface AdminReport {
  id: number;
  reason?: string;
  description?: string;
  status?: number;
  statusText?: string;
  reporterName?: string;
  postTitle?: string;
  createdAt?: string;
}

export const adminService = {
  getUsers: async (keyword?: string): Promise<ApiResponse<AdminUser[]>> =>
    (await api.get('/quan-tri/nguoi-dung', { params: { keyword } })).data,
  lockUser: async (id: number): Promise<ApiResponse<AdminUser>> =>
    (await api.put(`/quan-tri/nguoi-dung/${id}/khoa`)).data,
  unlockUser: async (id: number): Promise<ApiResponse<AdminUser>> =>
    (await api.put(`/quan-tri/nguoi-dung/${id}/mo-khoa`)).data,
  getPosts: async (keyword?: string, status?: number): Promise<ApiResponse<PostListItem[]>> =>
    (await api.get('/quan-tri/bai-dang', { params: { keyword, status } })).data,
  approvePost: async (id: number): Promise<ApiResponse<PostListItem>> =>
    (await api.put(`/quan-tri/bai-dang/${id}/duyet`)).data,
  rejectPost: async (id: number, reason: string): Promise<ApiResponse<PostListItem>> =>
    (await api.put(`/quan-tri/bai-dang/${id}/tu-choi`, { lyDo: reason, reason })).data,
  hidePost: async (id: number): Promise<ApiResponse<PostListItem>> =>
    (await api.put(`/quan-tri/bai-dang/${id}/an`)).data,
  getRooms: async (keyword?: string, status?: number): Promise<ApiResponse<AdminRoom[]>> =>
    (await api.get('/quan-tri/phong', { params: { keyword, status } })).data,
  getCategories: async (): Promise<ApiResponse<AdminCatalogItem[]>> =>
    (await api.get('/danh-muc', { params: { activeOnly: false } })).data,
  createCategory: async (data: { name: string; description?: string }): Promise<ApiResponse<AdminCatalogItem>> =>
    (await api.post('/danh-muc', data)).data,
  updateCategory: async (id: number, data: { name: string; description?: string }): Promise<ApiResponse<AdminCatalogItem>> =>
    (await api.put(`/danh-muc/${id}`, data)).data,
  deleteCategory: async (id: number): Promise<ApiResponse<null>> =>
    (await api.delete(`/danh-muc/${id}`)).data,
  getAmenities: async (): Promise<ApiResponse<AdminCatalogItem[]>> =>
    (await api.get('/tien-ich', { params: { activeOnly: false } })).data,
  createAmenity: async (data: { name: string; icon?: string; description?: string }): Promise<ApiResponse<AdminCatalogItem>> =>
    (await api.post('/tien-ich', data)).data,
  updateAmenity: async (id: number, data: { name: string; icon?: string; description?: string }): Promise<ApiResponse<AdminCatalogItem>> =>
    (await api.put(`/tien-ich/${id}`, data)).data,
  deleteAmenity: async (id: number): Promise<ApiResponse<null>> =>
    (await api.delete(`/tien-ich/${id}`)).data,
  getReports: async (status?: number): Promise<ApiResponse<AdminReport[]>> =>
    (await api.get('/bao-cao', { params: { status } })).data,
  updateReportStatus: async (id: number, status: number): Promise<ApiResponse<AdminReport>> =>
    (await api.put(`/bao-cao/${id}/trang-thai`, { status })).data,
};

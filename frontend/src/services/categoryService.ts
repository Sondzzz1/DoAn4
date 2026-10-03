import api from './api';
import { ApiResponse } from '../types/common.types';

export interface RoomCategory {
  id: number;
  name: string;
  description?: string | null;
  imageUrl?: string | null;
  isActive: boolean;
  roomCount: number;
}

export const categoryService = {
  getActiveCategories: async (): Promise<ApiResponse<RoomCategory[]>> =>
    (await api.get<ApiResponse<RoomCategory[]>>('/danh-muc')).data,
};

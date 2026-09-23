import api from './api';
import { ApiResponse } from '../types/common.types';
import { CreateRoomRequest, RoomItem, UpdateRoomRequest } from '../types/room.types';

export const roomService = {
  getMyRooms: async (status?: number): Promise<ApiResponse<RoomItem[]>> => {
    const response = await api.get<ApiResponse<RoomItem[]>>('/phong', { params: { status } });
    return response.data;
  },

  getRoomById: async (id: number): Promise<ApiResponse<RoomItem>> => {
    const response = await api.get<ApiResponse<RoomItem>>(`/phong/${id}`);
    return response.data;
  },

  createRoom: async (data: CreateRoomRequest): Promise<ApiResponse<RoomItem>> => {
    const response = await api.post<ApiResponse<RoomItem>>('/phong', data);
    return response.data;
  },

  updateRoom: async (id: number, data: UpdateRoomRequest): Promise<ApiResponse<RoomItem>> => {
    const response = await api.put<ApiResponse<RoomItem>>(`/phong/${id}`, data);
    return response.data;
  },

  updateRoomStatus: async (id: number, status: number): Promise<ApiResponse<RoomItem>> => {
    const response = await api.put<ApiResponse<RoomItem>>(`/phong/${id}/trang-thai`, { trangThai: String(status) });
    return response.data;
  },

  deleteRoom: async (id: number): Promise<ApiResponse<null>> => {
    const response = await api.delete<ApiResponse<null>>(`/phong/${id}`);
    return response.data;
  },
};

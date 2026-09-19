import api from './api';
import { ApiResponse } from '../types/common.types';

export interface CreateAppointmentRequest {
  baiDangId: number;
  chuTroId?: number;
  ngayXem: string;
  gioXem: string;
  ghiChu?: string;
}

export interface AppointmentItem {
  id: number;
  postId: number;
  postTitle: string;
  postAddress: string;
  postPrice: number;
  roomName?: string | null;
  tenantId: number;
  tenantAccountId: number;
  tenantName: string;
  tenantPhone: string;
  landlordId: number;
  landlordAccountId: number;
  landlordName: string;
  landlordPhone: string;
  scheduledAt: string;
  ngayXem: string;
  gioXem: string;
  status: number;
  statusText: string;
  tenantNote?: string | null;
  landlordResponse?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export const appointmentService = {
  createAppointment: async (data: CreateAppointmentRequest): Promise<ApiResponse<AppointmentItem>> => {
    const response = await api.post<ApiResponse<AppointmentItem>>('/lich-hen-xem-phong', data);
    return response.data;
  },

  getMyAppointments: async (): Promise<ApiResponse<AppointmentItem[]>> => {
    const response = await api.get<ApiResponse<AppointmentItem[]>>('/lich-hen-xem-phong/cua-toi');
    return response.data;
  },

  cancelAppointment: async (id: number, reason?: string): Promise<ApiResponse<AppointmentItem>> => {
    const response = await api.put<ApiResponse<AppointmentItem>>(`/lich-hen-xem-phong/${id}/huy`, reason ? { reason } : {});
    return response.data;
  },
};

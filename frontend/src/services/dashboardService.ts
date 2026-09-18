import api from './api';
import { ApiResponse } from '../types/common.types';

export interface LandlordDashboard {
  totalRooms: number;
  availableRooms: number;
  rentedRooms: number;
  maintenanceRooms: number;
  totalPosts: number;
  pendingPosts: number;
  approvedPosts: number;
  rejectedPosts: number;
  todayAppointments: number;
  pendingAppointments: number;
  totalAppointments: number;
  totalViewCount: number;
}

export interface AdminDashboard {
  totalUsers: number;
  totalTenants: number;
  totalLandlords: number;
  totalRooms: number;
  totalPosts: number;
  pendingPosts: number;
  approvedPosts: number;
  rejectedPosts: number;
  hiddenPosts: number;
  totalAppointments: number;
  totalReports: number;
  pendingReports: number;
  totalBlogs: number;
}

export const dashboardService = {
  getLandlord: async (): Promise<ApiResponse<LandlordDashboard>> => {
    const response = await api.get<ApiResponse<LandlordDashboard>>('/chu-tro/tong-quan');
    return response.data;
  },
  getAdmin: async (): Promise<ApiResponse<AdminDashboard>> => {
    const response = await api.get<ApiResponse<AdminDashboard>>('/quan-tri/tong-quan');
    return response.data;
  },
};

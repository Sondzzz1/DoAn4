import api from './api';
import type { ApiResponse } from '../types/common.types';

export interface LocationResult {
  displayName: string;
  latitude: number;
  longitude: number;
  address: string;
  province: string;
  district: string;
  ward: string;
}

export function buildLocationQuery(address = '', ward = '', district = '', province = '') {
  return [...new Set([address, ward, district, province, 'Việt Nam'].map(part => part.trim()).filter(Boolean))].join(', ');
}

export const locationService = {
  async search(query: string, signal: AbortSignal) {
    const response = await api.get<ApiResponse<LocationResult[]>>('/location/search', { params: { q: query }, signal, timeout: 20000 });
    if (!response.data.success) throw new Error(response.data.message || 'Không thể tìm vị trí.');
    return response.data.data || [];
  },
  async reverse(latitude: number, longitude: number, signal: AbortSignal) {
    const response = await api.get<ApiResponse<LocationResult>>('/location/reverse', { params: { lat: latitude, lng: longitude }, signal, timeout: 20000 });
    if (!response.data.success || !response.data.data) throw new Error(response.data.message || 'Không tìm thấy địa chỉ.');
    return response.data.data;
  },
};

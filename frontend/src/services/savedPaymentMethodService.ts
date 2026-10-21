import api from './api';
import type { ApiResponse } from '../types/common.types';

export interface SavedPaymentMethodDto {
  id: number;
  bankCode: string;
  maskedCardNumber: string;
  cardType: '01' | '02';
  createdAtUtc: string;
}

export interface SavedPaymentMethodsDto {
  enabled: boolean;
  unavailableReason?: string;
  methods: SavedPaymentMethodDto[];
}

export const SAVED_CARD_UNAVAILABLE_MESSAGE = 'Tính năng liên kết thẻ chưa được bật cho tài khoản VNPay của hệ thống. Bạn vẫn có thể thanh toán VNPay như bình thường.';

export const savedPaymentMethodService = {
  list: async (): Promise<SavedPaymentMethodsDto> => {
    const response = await api.get<ApiResponse<SavedPaymentMethodsDto>>('/thanh-toan/the-da-luu');
    const data = response.data.data;
    return { enabled: data?.enabled === true, unavailableReason: data?.unavailableReason, methods: Array.isArray(data?.methods) ? data.methods : [] };
  },
  link: async (cardType: '01' | '02'): Promise<{ redirectUrl: string; reference: string }> => {
    const response = await api.post<ApiResponse<{ redirectUrl: string; reference: string }>>('/thanh-toan/the-da-luu/lien-ket', { cardType });
    if (!response.data.data?.redirectUrl) throw new Error('Không nhận được liên kết từ VNPay.');
    return response.data.data;
  },
  remove: async (id: number): Promise<void> => {
    await api.delete(`/thanh-toan/the-da-luu/${id}`);
  },
};

import api from './api';
import { ApiResponse } from '../types/common.types';

export interface CreatePaymentRequest {
  depositId: number;
  orderInfo?: string;
}

export interface PaymentResponse {
  paymentUrl: string;
  orderId: string;
  amount: number;
}

export const paymentService = {
  createVnPayUrl: async (data: CreatePaymentRequest): Promise<ApiResponse<PaymentResponse>> => {
    const response = await api.post<ApiResponse<PaymentResponse>>('/thanh-toan/vnpay-tao-url', data);
    return response.data;
  },
};

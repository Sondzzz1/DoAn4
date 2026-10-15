import api from './api';
import type { ApiResponse } from '../types/common.types';

export interface RefundItem {
  id: number; depositId: number; rentalRequestId: number; roomName: string; amount: number; status: number;
  reason: string; decisionNote?: string; refundReference?: string; requestedAtUtc: string;
  refundReportedAtUtc?: string; completedAtUtc?: string; rowVersion: string;
}
export interface PaymentEventItem {
  id: number; orderId: string; transactionCode?: string; reportedAmount: number; expectedAmount?: number;
  source: number; responseCode: string; providerStatus: string; processingState: number; reason?: string;
  receivedAtUtc: string; providerPaidAtUtc?: string; resolvedAtUtc?: string; resolutionNote?: string; rowVersion: string;
}
export const paymentSafetyService = {
  refunds: async () => (await api.get<ApiResponse<RefundItem[]>>('/hoan-coc')).data.data || [],
  createRefund: async (depositId: number, reason: string) => (await api.post('/hoan-coc', { depositId, reason })).data,
  act: async (item: RefundItem, action: string, note: string, reference: string) =>
    (await api.put(`/hoan-coc/${item.id}/${action}`, { note, reference, rowVersion: item.rowVersion })).data,
  events: async (page: number) => (await api.get<ApiResponse<PaymentEventItem[]>>('/thanh-toan/su-kien', { params: { page } })).data.data || [],
  resolve: async (item: PaymentEventItem, note: string) =>
    (await api.put(`/thanh-toan/su-kien/${item.id}/doi-soat`, { note, rowVersion: item.rowVersion })).data,
};

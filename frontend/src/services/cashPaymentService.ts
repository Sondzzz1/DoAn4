import api from './api';
import type { ApiResponse } from '../types/common.types';

export const CASH_PAYMENT_STATUS = { SELECTED: 0, RECEIVED: 1, CANCELLED: 2 } as const;

export interface CashPaymentTarget {
  depositId?: number;
  monthlyBillId?: number;
}

export interface CashPaymentDto extends CashPaymentTarget {
  id: number;
  amount: number;
  status: number;
  tenantAccountId: number;
  landlordAccountId: number;
  tenantName?: string;
  landlordName?: string;
  landlordPhone?: string;
  targetTitle: string;
  selectedAtUtc: string;
  confirmedAtUtc?: string;
  cancelledAtUtc?: string;
  receiptNumber?: string;
  note?: string;
  targetDueAt?: string;
  canConfirm?: boolean;
  unavailableReason?: string;
}

const endpoint = '/thanh-toan/tien-mat';

export const cashPaymentService = {
  list: async (): Promise<ApiResponse<CashPaymentDto[]>> => {
    const response = await api.get<ApiResponse<CashPaymentDto[]>>(endpoint);
    return response.data;
  },
  select: async (target: CashPaymentTarget): Promise<ApiResponse<CashPaymentDto>> => {
    const response = await api.post<ApiResponse<CashPaymentDto>>(`${endpoint}/chon`, target);
    return response.data;
  },
  confirm: async (id: number, note?: string): Promise<ApiResponse<CashPaymentDto>> => {
    const response = await api.post<ApiResponse<CashPaymentDto>>(`${endpoint}/${id}/xac-nhan`, { note });
    return response.data;
  },
  cancel: async (id: number): Promise<ApiResponse<CashPaymentDto>> => {
    const response = await api.post<ApiResponse<CashPaymentDto>>(`${endpoint}/${id}/huy`, {});
    return response.data;
  },
};

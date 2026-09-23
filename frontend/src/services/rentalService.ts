import api from './api';
import { ApiResponse } from '../types/common.types';

export interface RentalRequestDto {
  id: number;
  baiDangId: number;
  nguoiThueId: number;
  chuTroId: number;
  tieuDeBaiDang?: string;
  trangThai: number; // 0: Chờ duyệt, 1: Đã duyệt, 2: Từ chối
  ghiChu?: string;
  ngayTao: string;
}

export interface DepositDto {
  id: number;
  yeuCauThueId: number;
  soTien: number;
  trangThai: number; // 0: Chờ thanh toán, 1: Đã thanh toán, 2: Đã hoàn cọc/hủy
  ngayThanhToan?: string;
  ngayTao: string;
}

export interface ContractDto {
  id: number;
  yeuCauThueId: number;
  baiDangId: number;
  nguoiThueId: number;
  chuTroId: number;
  ngayBatDau: string;
  ngayKetThuc: string;
  tienThueHangThang: number;
  trangThai: number; // 0: Chờ xác nhận, 1: Đang hiệu lực, 2: Đã kết thúc
  nguoiThueDaXacNhan: boolean;
  chuTroDaXacNhan: boolean;
}

export interface IncidentDto {
  id: number;
  hopDongId: number;
  nguoiBaoCaoId: number;
  tieuDe: string;
  moTa: string;
  trangThai: number; // 0: Chờ xử lý, 1: Đang xử lý, 2: Đã giải quyết
  huongXuLy?: string;
  ngayTao: string;
}

export interface ReviewDto {
  id: number;
  hopDongId: number;
  baiDangId: number;
  nguoiThueId: number;
  soSao: number;
  nhanXet?: string;
  ngayTao: string;
}

export const rentalService = {
  // Yêu cầu thuê
  createRentalRequest: async (data: { baiDangId: number; ghiChu?: string }): Promise<ApiResponse<RentalRequestDto>> => {
    const response = await api.post<ApiResponse<RentalRequestDto>>('/yeu-cau-thue', data);
    return response.data;
  },

  getMyRentalRequests: async (): Promise<ApiResponse<RentalRequestDto[]>> => {
    const response = await api.get<ApiResponse<RentalRequestDto[]>>('/yeu-cau-thue/cua-toi');
    return response.data;
  },

  // Đặt cọc
  createDeposit: async (data: { yeuCauThueId: number; soTien: number }): Promise<ApiResponse<DepositDto>> => {
    const response = await api.post<ApiResponse<DepositDto>>('/dat-coc', data);
    return response.data;
  },

  getMyDeposits: async (): Promise<ApiResponse<DepositDto[]>> => {
    const response = await api.get<ApiResponse<DepositDto[]>>('/dat-coc/cua-toi');
    return response.data;
  },

  // Hợp đồng
  getMyContracts: async (): Promise<ApiResponse<ContractDto[]>> => {
    const response = await api.get<ApiResponse<ContractDto[]>>('/hop-dong/cua-toi');
    return response.data;
  },

  confirmContract: async (id: number): Promise<ApiResponse<ContractDto>> => {
    const response = await api.put<ApiResponse<ContractDto>>(`/hop-dong/${id}/xac-nhan`);
    return response.data;
  },

  // Sự cố
  createIncident: async (data: { hopDongId: number; tieuDe: string; moTa: string }): Promise<ApiResponse<IncidentDto>> => {
    const response = await api.post<ApiResponse<IncidentDto>>('/su-co', data);
    return response.data;
  },

  getMyIncidents: async (): Promise<ApiResponse<IncidentDto[]>> => {
    const response = await api.get<ApiResponse<IncidentDto[]>>('/su-co/cua-toi');
    return response.data;
  },

  // Đánh giá
  createReview: async (data: { hopDongId: number; soSao: number; nhanXet?: string }): Promise<ApiResponse<ReviewDto>> => {
    const response = await api.post<ApiResponse<ReviewDto>>('/danh-gia', data);
    return response.data;
  },

  getReviewsByPost: async (baiDangId: number): Promise<ApiResponse<ReviewDto[]>> => {
    const response = await api.get<ApiResponse<ReviewDto[]>>(`/danh-gia/bai-dang/${baiDangId}`);
    return response.data;
  },

  getMyReviews: async (): Promise<ApiResponse<ReviewDto[]>> => {
    const response = await api.get<ApiResponse<ReviewDto[]>>('/danh-gia/cua-toi');
    return response.data;
  },
};

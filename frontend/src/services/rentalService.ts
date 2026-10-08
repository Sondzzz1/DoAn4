import api from './api';
import { ApiResponse } from '../types/common.types';

export interface RentalRequestDto {
  id: number;
  baiDangId: number;
  nguoiThueId: number;
  chuTroId: number;
  tieuDeBaiDang?: string;
  anhPhong?: string;
  giaThue?: number;
  diaChi?: string;
  tenNguoiThue?: string;
  sdtNguoiThue?: string;
  trangThai: number; // 0 pending, 1 approved, 2 rejected, 3 cancelled, 4 converted
  ghiChu?: string;
  lyDoHuy?: string;
  ngayTao: string;
}

export interface DepositDto {
  id: number;
  yeuCauThueId: number;
  soTien: number;
  trangThai: number; // 0 pending, 1 paid, 2 confirmed, 3 refund requested, 4 refunded, 5 cancelled
  hanThanhToan?: string;
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
  tienDatCoc?: number;
  giaDien: number;
  giaNuoc: number;
  phiDichVu: number;
  dieuKhoan?: string;
  trangThai: number; // 0: Chờ xác nhận, 1: Đang hiệu lực, 2: Đã kết thúc
  nguoiThueDaXacNhan: boolean;
  chuTroDaXacNhan: boolean;
  ngayTao?: string;
  tenPhong?: string;
  diaChiPhong?: string;
  tenNguoiThue?: string;
  sdtNguoiThue?: string;
  tenChuTro?: string;
  sdtChuTro?: string;
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

  getMyRentalRequests: async (isLandlord?: boolean): Promise<ApiResponse<RentalRequestDto[]>> => {
    const endpoint = isLandlord ? '/yeu-cau-thue/chu-tro' : '/yeu-cau-thue/cua-toi';
    const response = await api.get<ApiResponse<RentalRequestDto[]>>(endpoint);
    return response.data;
  },

  updateRentalRequestStatus: async (
    id: number,
    trangThai: number,
    ghiChu?: string,
    depositTerms?: { soTienDatCoc: number; hanThanhToanCoc: string },
  ): Promise<ApiResponse<RentalRequestDto>> => {
    const response = await api.put<ApiResponse<RentalRequestDto>>(`/yeu-cau-thue/${id}/trang-thai`, { trangThai, ghiChu, ...depositTerms });
    return response.data;
  },

  cancelRentalRequest: async (id: number): Promise<ApiResponse<RentalRequestDto>> => {
    const response = await api.put<ApiResponse<RentalRequestDto>>(`/yeu-cau-thue/${id}/huy`);
    return response.data;
  },

  // Compatibility repair for historical Approved requests missing a deposit.
  repairLegacyDeposit: async (requestId: number, data: { soTien: number; hanThanhToan: string }): Promise<ApiResponse<DepositDto>> => {
    const response = await api.post<ApiResponse<DepositDto>>(`/yeu-cau-thue/${requestId}/dat-coc`, data);
    return response.data;
  },

  getMyDeposits: async (): Promise<ApiResponse<DepositDto[]>> => {
    const response = await api.get<ApiResponse<DepositDto[]>>('/dat-coc/cua-toi');
    return response.data;
  },

  confirmDeposit: async (id: number): Promise<ApiResponse<DepositDto>> => {
    const response = await api.put<ApiResponse<DepositDto>>(`/dat-coc/${id}/trang-thai`, { trangThai: 2 });
    return response.data;
  },

  // Hợp đồng
  createContract: async (data: { yeuCauThueId: number; ngayBatDau: string; ngayKetThuc: string; tienThueHangThang: number; dieuKhoan?: string }): Promise<ApiResponse<ContractDto>> => {
    const response = await api.post<ApiResponse<ContractDto>>('/hop-dong', data);
    return response.data;
  },

  getMyContracts: async (): Promise<ApiResponse<ContractDto[]>> => {
    const response = await api.get<ApiResponse<ContractDto[]>>('/hop-dong/cua-toi');
    return response.data;
  },

  getContractDetail: async (id: number): Promise<ApiResponse<ContractDto>> => {
    const response = await api.get<ApiResponse<ContractDto>>(`/hop-dong/${id}`);
    return response.data;
  },

  confirmContract: async (id: number): Promise<ApiResponse<ContractDto>> => {
    const response = await api.put<ApiResponse<ContractDto>>(`/hop-dong/${id}/xac-nhan`);
    return response.data;
  },

  terminateContract: async (id: number, lyDo?: string): Promise<ApiResponse<ContractDto>> => {
    const response = await api.put<ApiResponse<ContractDto>>(`/hop-dong/${id}/cham-dut`, { lyDo });
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

import api from './api';
import { ApiResponse } from '../types/common.types';

export interface MonthlyBillDto {
  id: number;
  hopDongId: number;
  thang: number;
  nam: number;
  soDienCu: number;
  soDienMoi: number;
  soDienTieuThu: number;
  giaDien: number;
  tienDien: number;
  soNuocCu: number;
  soNuocMoi: number;
  soNuocTieuThu: number;
  giaNuoc: number;
  tienNuoc: number;
  tienPhong: number;
  chiPhiKhac: number;
  ghiChuChiPhiKhac?: string;
  tongTien: number;
  trangThai: number; // 0: Chờ thanh toán, 1: Đã thanh toán, 2: Đã hủy
  hanThanhToan?: string;
  ngayThanhToan?: string;
  phuongThucThanhToan?: string;
  ghiChu?: string;
  ngayTao: string;
  tenPhong?: string;
  diaChiPhong?: string;
  tenNguoiThue?: string;
  sdtNguoiThue?: string;
  tenChuTro?: string;
  sdtChuTro?: string;
}

export interface CreateMonthlyBillDto {
  hopDongId: number;
  thang: number;
  nam: number;
  soDienCu: number;
  soDienMoi: number;
  giaDien: number;
  soNuocCu: number;
  soNuocMoi: number;
  giaNuoc: number;
  tienPhong?: number;
  chiPhiKhac: number;
  ghiChuChiPhiKhac?: string;
  hanThanhToan?: string;
  ghiChu?: string;
}

export interface UpdateMonthlyBillDto {
  soDienCu: number;
  soDienMoi: number;
  giaDien: number;
  soNuocCu: number;
  soNuocMoi: number;
  giaNuoc: number;
  tienPhong: number;
  chiPhiKhac: number;
  ghiChuChiPhiKhac?: string;
  trangThai?: number;
  hanThanhToan?: string;
  ghiChu?: string;
}

export const monthlyBillService = {
  // Tạo hóa đơn mới
  createBill: async (data: CreateMonthlyBillDto): Promise<ApiResponse<MonthlyBillDto>> => {
    const response = await api.post<ApiResponse<MonthlyBillDto>>('/hoa-don', data);
    return response.data;
  },

  // Lấy danh sách hóa đơn theo hợp đồng
  getBillsByContract: async (contractId: number): Promise<ApiResponse<MonthlyBillDto[]>> => {
    const response = await api.get<ApiResponse<MonthlyBillDto[]>>(`/hoa-don/hop-dong/${contractId}`);
    return response.data;
  },

  // Lấy tất cả hóa đơn của tôi
  getMyBills: async (isLandlord?: boolean): Promise<ApiResponse<MonthlyBillDto[]>> => {
    const response = await api.get<ApiResponse<MonthlyBillDto[]>>('/hoa-don/cua-toi', {
      params: isLandlord !== undefined ? { isLandlord } : undefined,
    });
    return response.data;
  },

  // Lấy chi tiết một hóa đơn
  getBillDetail: async (id: number): Promise<ApiResponse<MonthlyBillDto>> => {
    const response = await api.get<ApiResponse<MonthlyBillDto>>(`/hoa-don/${id}`);
    return response.data;
  },

  // Cập nhật chỉ số / hóa đơn
  updateBill: async (id: number, data: UpdateMonthlyBillDto): Promise<ApiResponse<MonthlyBillDto>> => {
    const response = await api.put<ApiResponse<MonthlyBillDto>>(`/hoa-don/${id}`, data);
    return response.data;
  },

  // Xác nhận thanh toán hóa đơn
  payBill: async (id: number, data?: { phuongThucThanhToan?: string; ghiChu?: string }): Promise<ApiResponse<MonthlyBillDto>> => {
    const response = await api.put<ApiResponse<MonthlyBillDto>>(`/hoa-don/${id}/thanh-toan`, data);
    return response.data;
  },

  // Xóa hóa đơn
  deleteBill: async (id: number): Promise<ApiResponse<boolean>> => {
    const response = await api.delete<ApiResponse<boolean>>(`/hoa-don/${id}`);
    return response.data;
  },
};

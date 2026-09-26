import api from './api';
import { toast } from 'react-toastify';

export const exportService = {
  // Tải hợp đồng thuê phòng dạng PDF
  downloadContractPdf: async (contractId: number): Promise<void> => {
    try {
      toast.info('Đang chuẩn bị file PDF hợp đồng...');
      const response = await api.get(`/hop-dong/${contractId}/xuat-pdf`, {
        responseType: 'blob',
      });

      const blob = new Blob([response.data], { type: 'application/pdf' });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `HopDongThuePhong_HD${String(contractId).padStart(4, '0')}.pdf`);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
      toast.success('Tải hợp đồng PDF thành công!');
    } catch (error: any) {
      console.error('Lỗi khi tải PDF hợp đồng:', error);
      toast.error('Không thể xuất file PDF hợp đồng.');
    }
  },

  // Xuất báo cáo doanh thu & hóa đơn dạng Excel (.xlsx)
  downloadRevenueExcel: async (year?: number, month?: number): Promise<void> => {
    try {
      toast.info('Đang tổng hợp dữ liệu và xuất báo cáo Excel...');
      const response = await api.get('/bao-cao/doanh-thu/xuat-excel', {
        params: { nam: year, thang: month },
        responseType: 'blob',
      });

      const blob = new Blob([response.data], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      const dateStr = new Date().toISOString().slice(0, 10).replace(/-/g, '');
      link.setAttribute('download', `BaoCaoDoanhThu_${dateStr}.xlsx`);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
      toast.success('Xuất file Excel báo cáo doanh thu thành công!');
    } catch (error: any) {
      console.error('Lỗi khi xuất Excel doanh thu:', error);
      toast.error('Không thể xuất file báo cáo Excel.');
    }
  },
};

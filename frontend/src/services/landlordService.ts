import api from './api';

export const landlordService = {
  getAppointments: async () => (await api.get('/lich-hen-xem-phong/chu-tro')).data,
  getRentalRequests: async () => (await api.get('/yeu-cau-thue/chu-tro')).data,
  getContracts: async () => (await api.get('/hop-dong/cua-toi')).data,
  getDeposits: async () => (await api.get('/dat-coc/cua-toi')).data,
  confirmAppointment: async (id: number) => (await api.put(`/lich-hen-xem-phong/${id}/xac-nhan`)).data,
  rejectAppointment: async (id: number, reason?: string) => (await api.put(`/lich-hen-xem-phong/${id}/tu-choi`, reason ? { reason } : {})).data,
  completeAppointment: async (id: number) => (await api.put(`/lich-hen-xem-phong/${id}/hoan-thanh`)).data,
  approveRentalRequest: async (id: number) => (await api.put(`/yeu-cau-thue/${id}/trang-thai`, { trangThai: 1 })).data,
  rejectRentalRequest: async (id: number) => (await api.put(`/yeu-cau-thue/${id}/trang-thai`, { trangThai: 2 })).data,
  confirmContract: async (id: number) => (await api.put(`/hop-dong/${id}/xac-nhan`)).data,
  confirmDeposit: async (id: number) => (await api.put(`/dat-coc/${id}/trang-thai`, { trangThai: 1 })).data,
};

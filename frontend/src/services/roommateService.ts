import api from './api';
import type { ApiResponse } from '../types/common.types';

export interface RoommatePost {
  id: number; contractId?: number | null; title: string; description: string; requirements?: string;
  rejectionReason?: string; estimatedShare?: number | null; requestedSlots: number; remainingRequestedSlots: number;
  status: number; creatorName: string; creatorAvatar?: string; roomName: string; address: string;
  province: string; district: string; area: number; maxOccupants: number; currentOccupants: number;
  availableSlots: number; imageUrls: string[]; amenities: string[]; createdAt: string;
}
export interface RoommateDraft { contractId: number; title: string; description: string; requestedSlots: number; estimatedShare?: number; requirements: string }
export interface RoommateRequest {
  id: number; roommatePostId: number; applicantTenantAccountId: number; applicantName: string;
  applicantAvatar?: string; message?: string; status: number; postTitle: string;
  creatorTenantAccountId: number; creatorName: string; createdAt: string;
}
export interface Occupant { id: number; tenantAccountId: number; tenantName: string; status: number; joinedAt: string; leftAt?: string }
export interface RoommateContract { contractId: number; roomName: string; contractStatus: number; maxOccupants: number; currentOccupants: number; availableSlots: number; canCreatePost: boolean; occupants: Occupant[] }
export interface RoommateResult { items: RoommatePost[]; totalCount: number; pageNumber: number; pageSize: number; totalPages: number }
async function get<T>(path: string, params?: object) { return (await api.get<ApiResponse<T>>('/o-ghep' + path, { params })).data.data; }
async function put<T>(path: string, data?: object) { return (await api.put<ApiResponse<T>>('/o-ghep' + path, data || {})).data.data; }
export const roommateService = {
  search: (keyword: string, pageNumber = 1) => get<RoommateResult>('', { keyword, pageNumber, pageSize: 12 }),
  detail: (id: number) => get<RoommatePost>('/' + id),
  posts: (landlord = false) => get<RoommatePost[]>(landlord ? '/chu-tro' : '/cua-toi'),
  contracts: () => get<RoommateContract[]>('/hop-dong'),
  contract: (id: number) => get<RoommateContract>('/hop-dong/' + id),
  create: async (draft: RoommateDraft) => (await api.post<ApiResponse<RoommatePost>>('/o-ghep', draft)).data.data,
  update: (id: number, draft: RoommateDraft) => put<RoommatePost>('/' + id, draft),
  moderate: (id: number, approve: boolean, reason?: string) => put<RoommatePost>(`/${id}/duyet`, { approve, reason }),
  close: (id: number) => put<RoommatePost>(`/${id}/dong`),
  apply: async (id: number, message: string) => (await api.post<ApiResponse<RoommateRequest>>(`/o-ghep/${id}/yeu-cau`, { message })).data.data,
  requests: (id: number) => get<RoommateRequest[]>(`/${id}/yeu-cau`),
  applications: () => get<RoommateRequest[]>('/ung-tuyen'),
  decide: (id: number, landlord: boolean, approve: boolean) => put<RoommateRequest>(`/yeu-cau/${id}/${landlord ? 'xac-nhan' : 'tiep-nhan'}`, { approve }),
  cancel: (id: number) => put<RoommateRequest>(`/yeu-cau/${id}/huy`),
  leave: (id: number) => put<RoommateContract>(`/nguoi-o/${id}/roi-phong`),
};
export const roommatePostLabels = ['Chờ chủ trọ duyệt', 'Đang tìm người', 'Bị từ chối', 'Đã đủ người', 'Đã đóng', 'Hết hạn'];
export const roommateRequestLabels = ['Chờ người thuê chính', 'Chờ chủ trọ xác nhận', 'Đã được duyệt', 'Bị từ chối', 'Đã hủy'];

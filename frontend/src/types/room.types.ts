export interface RoomListItem {
  id: number;
  title: string;
  price: number;
  area: number;
  imageUrl: string;
  address: string;
  ward?: string;
  district?: string;
  province: string;
  createdAt: string;
  category: 'room' | 'whole-house' | 'apartment' | 'shared';
  landlordName?: string;
  roomStatus?: string;
}

export interface RoomItem {
  id: number;
  landlordId: number;
  landlordName: string;
  categoryId: number;
  categoryName: string;
  roomName: string;
  description?: string | null;
  price: number;
  area: number;
  maxOccupants: number;
  currentOccupants: number;
  bedrooms?: number | null;
  bathrooms?: number | null;
  floor?: number | null;
  address: string;
  ward?: string | null;
  district?: string | null;
  province?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  electricityPrice?: number | null;
  waterPrice?: number | null;
  serviceFee?: number | null;
  status: number;
  statusText?: string;
  imageUrls: string[];
  amenityIds: number[];
  activePostId?: number | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateRoomRequest {
  tenPhong: string;
  moTa?: string;
  gia: number;
  dienTich: number;
  soNguoiToiDa: number;
  soPhongNgu?: number | null;
  soPhongTam?: number | null;
  tang?: number | null;
  diaChi: string;
  phuong?: string | null;
  quan?: string | null;
  thanhPho?: string | null;
  tienIchIds?: number[];
  danhSachAnh?: string[];
}

export type UpdateRoomRequest = CreateRoomRequest;

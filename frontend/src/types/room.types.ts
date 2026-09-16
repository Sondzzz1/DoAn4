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

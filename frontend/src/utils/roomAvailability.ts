import { RoomStatus } from '../types/post.types';

export function getRoomAvailabilityLabel(status: RoomStatus): string {
  switch (status) {
    case RoomStatus.Available: return 'Còn trống';
    case RoomStatus.Rented: return 'Hết phòng';
    case RoomStatus.Reserved: return 'Đã giữ chỗ';
    case RoomStatus.TemporarilyUnavailable: return 'Tạm ngưng cho thuê';
    default: return 'Không khả dụng';
  }
}

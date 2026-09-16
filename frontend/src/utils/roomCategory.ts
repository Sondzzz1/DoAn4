export type RoomCategory =
  | 'room'
  | 'whole-house'
  | 'apartment'
  | 'shared';

export interface RoomCategoryConfig {
  key: RoomCategory;
  name: string;
  title: string;
  description: string;
  emptyMessage: string;
}

export const ROOM_CATEGORIES: Record<
  RoomCategory,
  RoomCategoryConfig
> = {
  room: {
    key: 'room',
    name: 'Phòng trọ',
    title: 'Phòng trọ cho thuê toàn quốc',
    description: 'Tìm phòng trọ giá tốt, vị trí thuận tiện',
    emptyMessage: 'Không tìm thấy phòng trọ phù hợp.',
  },

  'whole-house': {
    key: 'whole-house',
    name: 'Nhà nguyên căn',
    title: 'Nhà nguyên căn cho thuê toàn quốc',
    description: 'Tìm nhà nguyên căn phù hợp với nhu cầu của bạn',
    emptyMessage: 'Không tìm thấy nhà nguyên căn phù hợp.',
  },

  apartment: {
    key: 'apartment',
    name: 'Căn hộ',
    title: 'Căn hộ cho thuê toàn quốc',
    description: 'Tìm căn hộ phù hợp với nhu cầu sinh sống',
    emptyMessage: 'Không tìm thấy căn hộ phù hợp.',
  },

  shared: {
    key: 'shared',
    name: 'Ở ghép',
    title: 'Tìm Người Ở Ghép - Nam, Nữ, Chi Phí Thấp Toàn Quốc',
    description: 'Tìm người ở ghép phù hợp, tiết kiệm chi phí',
    emptyMessage: 'Không tìm thấy tin ở ghép phù hợp.',
  },
};

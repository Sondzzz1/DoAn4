import React, { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { FiChevronRight } from 'react-icons/fi';

import RoomSearchBar from '../../components/room/RoomSearchBar';
import RoomFilterBar from '../../components/room/RoomFilterBar';
import RoomListCard from '../../components/room/RoomListCard';
import RoomSidebar from '../../components/room/RoomSidebar';

import {
  ROOM_CATEGORIES,
  RoomCategory,
} from '../../utils/roomCategory';

import { RoomListItem } from '../../types/room.types';

import './RoomListPage.css';

// Demo data
const DEMO_ROOMS: RoomListItem[] = [
  {
    id: 1,
    title: 'Cần tìm 1 bạn nữ ở ghép cùng (ở luôn)',
    price: 2450000,
    area: 25,
    imageUrl:
      'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800',
    address: 'Số 20, Ngõ 52 Phú Mỹ',
    district: 'Nam Từ Liêm',
    province: 'Hà Nội',
    createdAt: '2026-08-06',
    category: 'shared',
  },

  {
    id: 2,
    title:
      'TÌM NỮ Ở GHÉP 1:1 - Nhà nhỏ nguyên căn mới',
    price: 2600000,
    area: 32,
    imageUrl:
      'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800',
    address: 'Phường 10',
    district: 'Tân Bình',
    province: 'Hồ Chí Minh',
    createdAt: '2026-07-20',
    category: 'shared',
  },

  {
    id: 3,
    title:
      'Phòng trọ cao cấp đầy đủ nội thất, gần trường đại học',
    price: 3200000,
    area: 28,
    imageUrl:
      'https://images.unsplash.com/photo-1556020685-ae41abfc9365?w=800',
    address: 'Ngõ 120',
    district: 'Cầu Giấy',
    province: 'Hà Nội',
    createdAt: '2026-08-01',
    category: 'room',
  },

  {
    id: 4,
    title:
      'Nhà nguyên căn 3 tầng, đầy đủ nội thất',
    price: 8500000,
    area: 70,
    imageUrl:
      'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=800',
    address: 'Đường Nguyễn Trãi',
    district: 'Thanh Xuân',
    province: 'Hà Nội',
    createdAt: '2026-08-03',
    category: 'whole-house',
  },

  {
    id: 5,
    title:
      'Căn hộ 2 phòng ngủ, nội thất đầy đủ',
    price: 9500000,
    area: 65,
    imageUrl:
      'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800',
    address: 'Vinhomes',
    district: 'Nam Từ Liêm',
    province: 'Hà Nội',
    createdAt: '2026-08-04',
    category: 'apartment',
  },

  {
    id: 6,
    title: 'Tìm Nam ở ghép gần ĐH Bách Khoa',
    price: 1800000,
    area: 22,
    imageUrl:
      'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800',
    address: 'Ngõ 52 Tạ Quang Bửu',
    district: 'Hai Bà Trưng',
    province: 'Hà Nội',
    createdAt: '2026-08-05',
    category: 'shared',
  },

  {
    id: 7,
    title: 'Phòng trọ mini giá sinh viên',
    price: 1500000,
    area: 18,
    imageUrl:
      'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800',
    address: 'Ngách 15, Ngõ 100 Hoàng Quốc Việt',
    district: 'Cầu Giấy',
    province: 'Hà Nội',
    createdAt: '2026-08-07',
    category: 'room',
  },

  {
    id: 8,
    title: 'Nhà nguyên căn mặt tiền đường lớn',
    price: 12000000,
    area: 90,
    imageUrl:
      'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=800',
    address: 'Đường Láng',
    district: 'Đống Đa',
    province: 'Hà Nội',
    createdAt: '2026-08-08',
    category: 'whole-house',
  },

  {
    id: 9,
    title: 'Căn hộ dịch vụ full nội thất cao cấp',
    price: 7500000,
    area: 45,
    imageUrl:
      'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800',
    address: 'Royal City',
    district: 'Thanh Xuân',
    province: 'Hà Nội',
    createdAt: '2026-08-09',
    category: 'apartment',
  },
];

const RoomListPage: React.FC = () => {
  const { category } = useParams<{
    category?: string;
  }>();

  const [keyword, setKeyword] = useState('');

  const currentCategory: RoomCategory =
    category === 'whole-house' ||
    category === 'apartment' ||
    category === 'shared'
      ? category
      : 'room';

  const config = ROOM_CATEGORIES[currentCategory];

  const rooms = useMemo(() => {
    return DEMO_ROOMS.filter(
      (room) => room.category === currentCategory
    );
  }, [currentCategory]);

  const handleSearch = () => {
    console.log('Search:', keyword);
  };

  return (
    <div className="room-list-page">
      {/* =========================================
          SEARCH
      ========================================= */}
      <div className="room-list-search-wrapper">
        <RoomSearchBar
          keyword={keyword}
          setKeyword={setKeyword}
          onSearch={handleSearch}
        />
      </div>

      {/* =========================================
          FILTER
      ========================================= */}
      <div className="room-list-filter-wrapper">
        <RoomFilterBar
          categoryName={config.name}
        />
      </div>

      {/* =========================================
          MAIN
      ========================================= */}
      <main className="room-list-container">
        {/* Breadcrumb */}
        <nav className="room-list-breadcrumb">
          <Link to="/">
            Trang chủ
          </Link>

          <FiChevronRight />

          <span>
            {config.name}
          </span>
        </nav>

        {/* Layout */}
        <div className="room-list-layout">
          {/* LEFT */}
          <section className="room-list-main">
            <div className="room-list-heading">
              <div>
                <h1>
                  {config.title}
                </h1>

                <p>
                  Hiện có {rooms.length} tin
                </p>
              </div>

              <select className="room-sort-select">
                <option>Tin mới đăng</option>
                <option>Giá thấp đến cao</option>
                <option>Giá cao đến thấp</option>
                <option>Diện tích nhỏ đến lớn</option>
              </select>
            </div>

            {/* Room list */}
            <div className="room-list-items">
              {rooms.length > 0 ? (
                rooms.map((room) => (
                  <RoomListCard
                    key={room.id}
                    room={room}
                  />
                ))
              ) : (
                <div className="room-list-empty">
                  {config.emptyMessage}
                </div>
              )}
            </div>
          </section>

          {/* RIGHT */}
          <RoomSidebar
            latestRooms={rooms}
          />
        </div>
      </main>
    </div>
  );
};

export default RoomListPage;

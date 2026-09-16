import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { postService } from '../../services/postService';
import { PostListItem } from '../../types/post.types';
import { formatCurrency } from '../../utils/helpers';
import { ROUTES } from '../../utils/constants';
import { FiMapPin, FiHome, FiChevronRight } from 'react-icons/fi';

interface SimilarRoomsProps {
  currentPostId: number;
}

// Mock similar rooms data for fallback
const MOCK_SIMILAR_ROOMS: PostListItem[] = [
  {
    id: 2,
    title: 'Phòng trọ cao cấp quận Hai Bà Trưng - Full nội thất',
    price: 3500000,
    status: 1,
    area: 30,
    maxOccupants: 2,
    roomStatus: 0,
    province: 'Hà Nội',
    district: 'Hai Bà Trưng',
    ward: 'Bạch Đằng',
    thumbnailUrl: 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=600&h=400&fit=crop',
    createdAt: '2026-09-14T10:00:00Z',
    updatedAt: '2026-09-14T10:00:00Z',
  },
  {
    id: 3,
    title: 'Cho thuê phòng trọ giá rẻ gần ĐH Bách Khoa',
    price: 2000000,
    status: 1,
    area: 20,
    maxOccupants: 1,
    roomStatus: 0,
    province: 'Hà Nội',
    district: 'Đống Đa',
    ward: 'Khâm Thiên',
    thumbnailUrl: 'https://images.unsplash.com/photo-1556020685-ae41abfc9365?w=600&h=400&fit=crop',
    createdAt: '2026-09-13T14:30:00Z',
    updatedAt: '2026-09-13T14:30:00Z',
  },
  {
    id: 4,
    title: 'Phòng trọ ở ghép sinh viên quận Cầu Giấy',
    price: 1800000,
    status: 1,
    area: 18,
    maxOccupants: 2,
    roomStatus: 0,
    province: 'Hà Nội',
    district: 'Cầu Giấy',
    ward: 'Dịch Vọng',
    thumbnailUrl: 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=600&h=400&fit=crop',
    createdAt: '2026-09-12T09:00:00Z',
    updatedAt: '2026-09-12T09:00:00Z',
  },
  {
    id: 5,
    title: 'Căn hộ mini 1 phòng ngủ full nội thất',
    price: 4500000,
    status: 1,
    area: 35,
    maxOccupants: 2,
    roomStatus: 0,
    province: 'Hà Nội',
    district: 'Thanh Xuân',
    ward: 'Nhân Chính',
    thumbnailUrl: 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=600&h=400&fit=crop',
    createdAt: '2026-09-11T16:20:00Z',
    updatedAt: '2026-09-11T16:20:00Z',
  },
];

const SimilarRooms: React.FC<SimilarRoomsProps> = ({ currentPostId }) => {
  const [rooms, setRooms] = useState<PostListItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchSimilarRooms();
  }, [currentPostId]);

  const fetchSimilarRooms = async () => {
    setLoading(true);
    try {
      // TODO: Call API with filter params similar to current post
      const response = await postService.getPosts({});
      
      if (response?.data) {
        // Filter out current post and take first 4
        const filtered = response.data
          .filter((post: PostListItem) => post.id !== currentPostId)
          .slice(0, 4);
        setRooms(filtered);
      } else {
        // Use mock data if API fails
        setRooms(MOCK_SIMILAR_ROOMS.filter((room) => room.id !== currentPostId).slice(0, 4));
      }
    } catch (error) {
      console.warn('Failed to fetch similar rooms, using mock data:', error);
      setRooms(MOCK_SIMILAR_ROOMS.filter((room) => room.id !== currentPostId).slice(0, 4));
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="mt-12">
        <h2 className="text-[20px] font-bold text-gray-900 mb-5">Phòng trọ tương tự</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="bg-white rounded-2xl overflow-hidden border border-gray-100 animate-pulse">
              <div className="w-full h-48 bg-gray-200" />
              <div className="p-4 space-y-3">
                <div className="h-4 bg-gray-200 rounded w-3/4" />
                <div className="h-5 bg-gray-200 rounded w-1/2" />
                <div className="h-3 bg-gray-200 rounded w-full" />
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (!rooms || rooms.length === 0) {
    return null;
  }

  return (
    <div className="mt-12">
      <div className="flex items-center justify-between mb-5">
        <h2 className="text-[20px] font-bold text-gray-900">Phòng trọ tương tự</h2>
        <Link
          to={ROUTES.ROOM_LIST || '/rooms'}
          className="flex items-center gap-1 text-[14px] font-semibold text-[#0084ff] hover:gap-2 transition-all"
        >
          Xem tất cả
          <FiChevronRight className="w-4 h-4" />
        </Link>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        {rooms.map((room) => (
          <Link
            key={room.id}
            to={`${ROUTES.ROOM_DETAIL || '/rooms'}/${room.id}`}
            className="group bg-white rounded-2xl overflow-hidden border border-gray-100 hover:border-[#0084ff] hover:shadow-lg transition-all"
          >
            {/* Image */}
            <div className="relative w-full h-48 overflow-hidden bg-gray-100">
              <img
                src={room.thumbnailUrl || 'https://via.placeholder.com/600x400?text=No+Image'}
                alt={room.title}
                className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                loading="lazy"
              />
              <div className="absolute top-3 left-3 px-2.5 py-1 rounded-lg bg-[#00a651] text-white text-[12px] font-semibold">
                Còn trống
              </div>
            </div>

            {/* Content */}
            <div className="p-4">
              <h3 className="text-[15px] font-semibold text-gray-900 mb-2 line-clamp-2 group-hover:text-[#0084ff] transition-colors">
                {room.title}
              </h3>

              <div className="flex items-center gap-1.5 text-[13px] text-gray-600 mb-3">
                <FiMapPin className="w-3.5 h-3.5 flex-shrink-0" />
                <span className="truncate">{room.district}, {room.province}</span>
              </div>

              <div className="flex items-center justify-between pt-3 border-t border-gray-100">
                <span className="text-[17px] font-bold text-[#0084ff]">
                  {formatCurrency(room.price)}
                </span>
                <div className="flex items-center gap-1 text-[13px] text-gray-500">
                  <FiHome className="w-3.5 h-3.5" />
                  <span>{room.area} m²</span>
                </div>
              </div>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
};

export default SimilarRooms;

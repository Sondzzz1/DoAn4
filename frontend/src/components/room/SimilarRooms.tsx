import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { FiChevronRight, FiHome, FiMapPin } from 'react-icons/fi';
import { postService } from '../../services/postService';
import { PostListItem } from '../../types/post.types';
import { formatCurrency } from '../../utils/helpers';
import { ROUTES } from '../../utils/constants';

interface SimilarRoomsProps {
  currentPostId: number;
  categoryId: number;
}

const SimilarRooms: React.FC<SimilarRoomsProps> = ({ currentPostId, categoryId }) => {
  const [rooms, setRooms] = useState<PostListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [hasError, setHasError] = useState(false);

  useEffect(() => {
    let active = true;

    const loadSimilarRooms = async () => {
      setLoading(true);
      setHasError(false);

      try {
        const response = await postService.getPosts({ categoryId, pageSize: 5 });
        if (!active) return;

        setRooms((response.data || []).filter(room => room.id !== currentPostId).slice(0, 4));
      } catch {
        if (active) {
          setRooms([]);
          setHasError(true);
        }
      } finally {
        if (active) setLoading(false);
      }
    };

    void loadSimilarRooms();
    return () => { active = false; };
  }, [categoryId, currentPostId]);

  if (loading) {
    return (
      <div className="mt-12">
        <h2 className="mb-5 text-[20px] font-bold text-gray-900">Phòng trọ tương tự</h2>
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }, (_, index) => (
            <div key={index} className="overflow-hidden rounded-lg border border-gray-100 bg-white animate-pulse">
              <div className="h-48 w-full bg-gray-200" />
              <div className="space-y-3 p-4"><div className="h-4 w-3/4 bg-gray-200" /><div className="h-5 w-1/2 bg-gray-200" /></div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (hasError || rooms.length === 0) return null;

  return (
    <div className="mt-12">
      <div className="mb-5 flex items-center justify-between">
        <h2 className="text-[20px] font-bold text-gray-900">Phòng trọ tương tự</h2>
        <Link to={ROUTES.ROOM_LIST || '/rooms'} className="flex items-center gap-1 text-[14px] font-semibold text-[#0084ff] transition-all hover:gap-2">
          Xem tất cả
          <FiChevronRight className="h-4 w-4" />
        </Link>
      </div>

      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
        {rooms.map(room => (
          <Link key={room.id} to={`${ROUTES.ROOM_DETAIL || '/rooms'}/${room.id}`} className="group overflow-hidden rounded-lg border border-gray-100 bg-white transition-all hover:border-[#0084ff] hover:shadow-lg">
            <div className="relative h-48 w-full overflow-hidden bg-gray-100">
              {room.thumbnailUrl ? (
                <img src={room.thumbnailUrl} alt={room.title} className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105" loading="lazy" />
              ) : (
                <div className="flex h-full items-center justify-center text-sm text-gray-500">Chưa có ảnh</div>
              )}
              <div className="absolute left-3 top-3 rounded bg-[#00a651] px-2.5 py-1 text-[12px] font-semibold text-white">Còn trống</div>
            </div>

            <div className="p-4">
              <h3 className="mb-2 line-clamp-2 text-[15px] font-semibold text-gray-900 transition-colors group-hover:text-[#0084ff]">{room.title}</h3>
              <div className="mb-3 flex items-center gap-1.5 text-[13px] text-gray-600"><FiMapPin className="h-3.5 w-3.5 flex-shrink-0" /><span className="truncate">{room.district}, {room.province}</span></div>
              <div className="flex items-center justify-between border-t border-gray-100 pt-3"><span className="text-[17px] font-bold text-[#0084ff]">{formatCurrency(room.price)}</span><div className="flex items-center gap-1 text-[13px] text-gray-500"><FiHome className="h-3.5 w-3.5" /><span>{room.area} m²</span></div></div>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
};

export default SimilarRooms;

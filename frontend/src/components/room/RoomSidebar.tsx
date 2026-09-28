import React from 'react';
import { Link } from 'react-router-dom';
import { FiChevronRight } from 'react-icons/fi';
import { RoomListItem } from '../../types/room.types';

interface RoomSidebarProps {
  latestRooms: RoomListItem[];
}

const priceRanges = [
  'Dưới 1 triệu',
  '1 - 2 triệu',
  '2 - 3 triệu',
  '3 - 5 triệu',
  '5 - 7 triệu',
  '7 - 10 triệu',
  '10 - 15 triệu',
  'Trên 15 triệu',
];

const areaRanges = [
  'Dưới 20m²',
  '20 - 30m²',
  '30 - 50m²',
  '50 - 70m²',
  '70 - 90m²',
  'Trên 90m²',
];

const RoomSidebar: React.FC<RoomSidebarProps> = ({ latestRooms }) => {
  const formatPrice = (price: number) => {
    return `${(price / 1000000).toFixed(1)} tr/tháng`;
  };

  return (
    <aside className="space-y-6">
      {/* Price & Area Filters */}
      <div className="bg-white rounded-2xl border-2 border-slate-200 p-6 shadow-md">
        <h3 className="text-lg font-bold text-slate-900 mb-5 pb-3 border-b-2 border-slate-100">
          Lọc theo khoảng giá
        </h3>
        <div className="grid grid-cols-2 gap-3 mb-6">
          {priceRanges.map((range) => (
            <Link
              key={range}
              to="#"
              className="flex items-center gap-2 px-3 py-2.5 text-sm font-semibold text-slate-700 bg-slate-50 hover:bg-blue-50 hover:text-blue-700 rounded-xl border-2 border-slate-100 hover:border-blue-200 transition-all"
            >
              <FiChevronRight className="text-blue-500 flex-shrink-0" size={16} />
              <span className="truncate">{range}</span>
            </Link>
          ))}
        </div>

        <h3 className="text-lg font-bold text-slate-900 mb-5 pb-3 border-b-2 border-slate-100 mt-6">
          Lọc theo diện tích
        </h3>
        <div className="grid grid-cols-2 gap-3">
          {areaRanges.map((range) => (
            <Link
              key={range}
              to="#"
              className="flex items-center gap-2 px-3 py-2.5 text-sm font-semibold text-slate-700 bg-slate-50 hover:bg-blue-50 hover:text-blue-700 rounded-xl border-2 border-slate-100 hover:border-blue-200 transition-all"
            >
              <FiChevronRight className="text-blue-500 flex-shrink-0" size={16} />
              <span className="truncate">{range}</span>
            </Link>
          ))}
        </div>
      </div>

      {/* Latest Rooms */}
      <div className="bg-white rounded-2xl border-2 border-slate-200 p-6 shadow-md">
        <h3 className="text-lg font-bold text-slate-900 mb-5 pb-3 border-b-2 border-slate-100">
          Tin mới đăng
        </h3>
        <div className="space-y-4">
          {latestRooms.slice(0, 5).map((room) => (
            <Link
              key={room.id}
              to={`/rooms/${room.id}`}
              className="flex gap-4 group hover:bg-slate-50 p-2 rounded-xl transition-all -mx-2"
            >
              {/* Thumbnail */}
              <div className="w-24 h-20 flex-shrink-0 rounded-lg overflow-hidden bg-slate-100">
                {room.imageUrl ? (
                  <img
                    src={room.imageUrl}
                    alt={room.title}
                    className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-300"
                  />
                ) : (
                  <div className="w-full h-full flex items-center justify-center text-slate-300 text-2xl">
                    🏠
                  </div>
                )}
              </div>

              {/* Info */}
              <div className="flex-1 min-w-0">
                <h4 className="text-sm font-bold text-slate-900 line-clamp-2 mb-2 group-hover:text-blue-600 transition-colors leading-tight">
                  {room.title}
                </h4>
                <div className="flex items-center gap-2 text-xs mb-1">
                  <span className="font-bold text-blue-600">{formatPrice(room.price)}</span>
                  <span className="text-slate-300">•</span>
                  <span className="text-slate-600 font-semibold">{room.area} m²</span>
                </div>
                <p className="text-xs text-slate-500">
                  {new Date(room.createdAt).toLocaleDateString('vi-VN')}
                </p>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </aside>
  );
};

export default RoomSidebar;

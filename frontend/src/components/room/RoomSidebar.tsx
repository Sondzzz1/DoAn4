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

const RoomSidebar: React.FC<RoomSidebarProps> = ({
  latestRooms,
}) => {
  const formatPrice = (price: number) => {
    return `${(price / 1000000).toFixed(2)} Triệu/tháng`;
  };

  return (
    <aside className="room-sidebar">
      {/* PRICE */}
      <div className="room-sidebar-box">
        <h3>Xem theo khoảng giá</h3>

        <div className="room-sidebar-grid">
          {priceRanges.map((item) => (
            <Link
              key={item}
              to="#"
              className="room-sidebar-link"
            >
              <FiChevronRight />
              <span>{item}</span>
            </Link>
          ))}
        </div>

        <h3 className="room-sidebar-subtitle">
          Xem theo diện tích
        </h3>

        <div className="room-sidebar-grid">
          {areaRanges.map((item) => (
            <Link
              key={item}
              to="#"
              className="room-sidebar-link"
            >
              <FiChevronRight />
              <span>{item}</span>
            </Link>
          ))}
        </div>
      </div>

      {/* LATEST */}
      <div className="room-sidebar-box">
        <h3 className="room-sidebar-latest-title">
          Tin mới đăng
        </h3>

        <div className="room-sidebar-latest">
          {latestRooms.slice(0, 4).map((room) => (
            <Link
              key={room.id}
              to={`/rooms/${room.id}`}
              className="room-sidebar-latest-item"
            >
              <img
                src={room.imageUrl}
                alt={room.title}
              />

              <div>
                <h4>{room.title}</h4>

                <div>
                  <strong>
                    {formatPrice(room.price)}
                  </strong>

                  <span>
                    {room.area} m²
                  </span>
                </div>

                <small>
                  {new Date(
                    room.createdAt
                  ).toLocaleDateString('vi-VN')}
                </small>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </aside>
  );
};

export default RoomSidebar;

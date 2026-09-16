import React from 'react';
import { Link } from 'react-router-dom';
import { FiClock, FiMapPin } from 'react-icons/fi';
import { RoomListItem } from '../../types/room.types';

interface RoomListCardProps {
  room: RoomListItem;
}

const RoomListCard: React.FC<RoomListCardProps> = ({ room }) => {
  const formatPrice = (price: number) => {
    return `${(price / 1000000).toFixed(2)} Triệu/tháng`;
  };

  const formatDate = (date: string) => {
    return new Date(date).toLocaleDateString('vi-VN');
  };

  return (
    <Link
      to={`/rooms/${room.id}`}
      className="room-list-card"
    >
      <div className="room-list-card-image">
        <img
          src={room.imageUrl}
          alt={room.title}
        />
      </div>

      <div className="room-list-card-content">
        <h3 className="room-list-card-title">
          {room.title}
        </h3>

        <div className="room-list-card-address">
          <FiMapPin />

          <span>
            {room.address}
            {room.district && `, ${room.district}`}
            {room.province && `, ${room.province}`}
          </span>
        </div>

        <div className="room-list-card-price-row">
          <span className="room-list-card-price">
            {formatPrice(room.price)}
          </span>

          <span className="room-list-card-dot">
            ·
          </span>

          <span className="room-list-card-area">
            {room.area} m²
          </span>
        </div>

        <div className="room-list-card-bottom">
          <span className="room-list-card-date">
            <FiClock />
            {formatDate(room.createdAt)}
          </span>
        </div>
      </div>
    </Link>
  );
};

export default RoomListCard;

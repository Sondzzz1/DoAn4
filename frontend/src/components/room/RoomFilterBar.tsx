import React from 'react';
import { FiChevronDown } from 'react-icons/fi';

interface RoomFilterBarProps {
  categoryName: string;
}

const RoomFilterBar: React.FC<RoomFilterBarProps> = ({
  categoryName,
}) => {
  return (
    <div className="room-filter-bar">
      <button className="room-filter-select">
        <span>{categoryName}</span>
        <FiChevronDown />
      </button>

      <button className="room-filter-select">
        <span>Tất cả giá</span>
        <FiChevronDown />
      </button>

      <button className="room-filter-select">
        <span>Tất cả diện tích</span>
        <FiChevronDown />
      </button>
    </div>
  );
};

export default RoomFilterBar;

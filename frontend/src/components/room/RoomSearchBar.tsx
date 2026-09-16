import React from 'react';
import { FiMapPin, FiSearch, FiChevronDown } from 'react-icons/fi';

interface RoomSearchBarProps {
  keyword: string;
  setKeyword: (value: string) => void;
  onSearch: () => void;
}

const RoomSearchBar: React.FC<RoomSearchBarProps> = ({
  keyword,
  setKeyword,
  onSearch,
}) => {
  return (
    <section className="room-search-section">
      <div className="room-search-location">
        <FiMapPin />

        <span>Toàn quốc</span>

        <FiChevronDown className="room-search-chevron" />
      </div>

      <div className="room-search-input-wrapper">
        <FiSearch />

        <input
          type="text"
          value={keyword}
          onChange={(e) => setKeyword(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              onSearch();
            }
          }}
          placeholder="Tìm kiếm theo địa điểm"
        />
      </div>

      <button
        type="button"
        className="room-search-button"
        onClick={onSearch}
      >
        Tìm kiếm
      </button>
    </section>
  );
};

export default RoomSearchBar;

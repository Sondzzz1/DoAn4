import React from 'react';
import { Amenity } from '../../types/post.types';
import {
  FiWifi,
  FiWind,
  FiDroplet,
  FiShield,
  FiTruck,
  FiClock,
  FiHome,
  FiCheckCircle,
} from 'react-icons/fi';

interface RoomAmenitiesProps {
  amenities: Amenity[];
}

// Icon mapping for common amenities
const getAmenityIcon = (name: string): React.ReactNode => {
  const lowerName = name.toLowerCase();
  
  if (lowerName.includes('wifi') || lowerName.includes('internet')) {
    return <FiWifi className="w-5 h-5" />;
  }
  if (lowerName.includes('điều hòa') || lowerName.includes('máy lạnh')) {
    return <FiWind className="w-5 h-5" />;
  }
  if (lowerName.includes('nóng lạnh') || lowerName.includes('nước')) {
    return <FiDroplet className="w-5 h-5" />;
  }
  if (lowerName.includes('camera') || lowerName.includes('an ninh')) {
    return <FiShield className="w-5 h-5" />;
  }
  if (lowerName.includes('xe') || lowerName.includes('gửi xe')) {
    return <FiTruck className="w-5 h-5" />;
  }
  if (lowerName.includes('giờ') || lowerName.includes('tự do')) {
    return <FiClock className="w-5 h-5" />;
  }
  if (lowerName.includes('giường') || lowerName.includes('tủ') || lowerName.includes('bàn')) {
    return <FiHome className="w-5 h-5" />;
  }
  
  return <FiCheckCircle className="w-5 h-5" />;
};

const RoomAmenities: React.FC<RoomAmenitiesProps> = ({ amenities }) => {
  if (!amenities || amenities.length === 0) {
    return null;
  }

  return (
    <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100">
      <h2 className="text-[18px] font-bold text-gray-900 mb-4">Tiện ích phòng trọ</h2>
      
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
        {amenities.map((amenity) => (
          <div
            key={amenity.id}
            className="flex items-center gap-3 p-3 rounded-xl bg-gray-50 hover:bg-gray-100 transition-colors"
          >
            <div className="w-10 h-10 rounded-lg bg-white flex items-center justify-center flex-shrink-0 text-[#0084ff]">
              {getAmenityIcon(amenity.name)}
            </div>
            <span className="text-[15px] font-medium text-gray-800">{amenity.name}</span>
          </div>
        ))}
      </div>
    </div>
  );
};

export default RoomAmenities;

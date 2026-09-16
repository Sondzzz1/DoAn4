
import React from 'react';
import { FiMapPin, FiNavigation } from 'react-icons/fi';

interface RoomLocationProps {
  address: string;
  ward?: string;
  district: string;
  province: string;
}

const RoomLocation: React.FC<RoomLocationProps> = ({
  address,
  ward,
  district,
  province,
}) => {
  const fullAddress = [address, ward, district, province]
    .filter(Boolean)
    .join(', ');

  // Tạo URL Google Maps từ địa chỉ
  const mapsUrl = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(
    fullAddress
  )}`;

  return (
    <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100">
      <h2 className="text-[18px] font-bold text-gray-900 mb-4">
        Vị trí & Địa chỉ
      </h2>

      {/* Thông tin địa chỉ */}
      <div className="flex items-start gap-3 mb-4 p-4 rounded-xl bg-gray-50">
        <div className="w-10 h-10 rounded-lg bg-white flex items-center justify-center flex-shrink-0">
          <FiMapPin className="w-5 h-5 text-[#0084ff]" />
        </div>

        <div className="flex-1">
          <p className="text-[15px] text-gray-700 leading-relaxed">
            {fullAddress}
          </p>
        </div>
      </div>

      {/* Khu vực bản đồ */}
      <div className="relative w-full h-[320px] rounded-xl overflow-hidden bg-gray-100 border border-gray-200">
        <div className="absolute inset-0 flex flex-col items-center justify-center text-center p-6">
          <div className="w-16 h-16 rounded-full bg-white flex items-center justify-center shadow-sm mb-4">
            <FiMapPin className="w-8 h-8 text-[#0084ff]" />
          </div>

          <p className="text-[15px] font-medium text-gray-700 mb-2">
            Vị trí phòng trọ
          </p>

          <p className="text-[14px] text-gray-500 mb-5 max-w-md">
            Xem vị trí chính xác của phòng trọ trên Google Maps
          </p>

          <a
            href={mapsUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#0084ff] text-white text-[14px] font-semibold hover:bg-[#0073df] transition-colors shadow-sm"
          >
            <FiNavigation className="w-4 h-4" />
            Mở Google Maps
          </a>
        </div>
      </div>

      {/* Chi tiết địa chỉ */}
      <div className="mt-4 grid grid-cols-1 sm:grid-cols-3 gap-3">
        <div className="p-3 rounded-xl bg-gray-50 text-center">
          <p className="text-[13px] text-gray-500 mb-1">
            Phường/Xã
          </p>

          <p className="text-[15px] font-semibold text-gray-900">
            {ward || 'N/A'}
          </p>
        </div>

        <div className="p-3 rounded-xl bg-gray-50 text-center">
          <p className="text-[13px] text-gray-500 mb-1">
            Quận/Huyện
          </p>

          <p className="text-[15px] font-semibold text-gray-900">
            {district}
          </p>
        </div>

        <div className="p-3 rounded-xl bg-gray-50 text-center">
          <p className="text-[13px] text-gray-500 mb-1">
            Tỉnh/Thành
          </p>

          <p className="text-[15px] font-semibold text-gray-900">
            {province}
          </p>
        </div>
      </div>
    </div>
  );
};

export default RoomLocation;

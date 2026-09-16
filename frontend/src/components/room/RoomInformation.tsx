import React from 'react';
import { Post } from '../../types/post.types';
import { FiHome, FiUsers, FiMapPin, FiDollarSign } from 'react-icons/fi';
import { formatCurrency } from '../../utils/helpers';

interface RoomInformationProps {
  post: Post;
}

const RoomInformation: React.FC<RoomInformationProps> = ({ post }) => {
  return (
    <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100">
      {/* Title and Price */}
      <div className="mb-6">
        <h1 className="text-[26px] font-bold text-gray-900 leading-tight mb-4">
          {post.title}
        </h1>
        <div className="flex items-baseline gap-2">
          <span className="text-[26px] font-bold text-[#0084ff]">
            {formatCurrency(post.price)}
          </span>
          <span className="text-[15px] text-gray-500">/ tháng</span>
        </div>
      </div>

      {/* Quick Info Grid */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6 pb-6 border-b border-gray-100">
        <div className="flex items-start gap-3">
          <div className="w-10 h-10 rounded-xl bg-blue-50 flex items-center justify-center flex-shrink-0">
            <FiHome className="w-5 h-5 text-[#0084ff]" />
          </div>
          <div>
            <p className="text-[13px] text-gray-500 mb-0.5">Diện tích</p>
            <p className="text-[15px] font-semibold text-gray-900">{post.area} m²</p>
          </div>
        </div>

        <div className="flex items-start gap-3">
          <div className="w-10 h-10 rounded-xl bg-green-50 flex items-center justify-center flex-shrink-0">
            <FiUsers className="w-5 h-5 text-[#00a651]" />
          </div>
          <div>
            <p className="text-[13px] text-gray-500 mb-0.5">Số người</p>
            <p className="text-[15px] font-semibold text-gray-900">
              {post.maxOccupants} {post.maxOccupants === 1 ? 'người' : 'người'}
            </p>
          </div>
        </div>

        <div className="flex items-start gap-3">
          <div className="w-10 h-10 rounded-xl bg-purple-50 flex items-center justify-center flex-shrink-0">
            <FiMapPin className="w-5 h-5 text-purple-500" />
          </div>
          <div>
            <p className="text-[13px] text-gray-500 mb-0.5">Khu vực</p>
            <p className="text-[15px] font-semibold text-gray-900">{post.district}</p>
          </div>
        </div>

        <div className="flex items-start gap-3">
          <div className="w-10 h-10 rounded-xl bg-amber-50 flex items-center justify-center flex-shrink-0">
            <FiDollarSign className="w-5 h-5 text-amber-500" />
          </div>
          <div>
            <p className="text-[13px] text-gray-500 mb-0.5">Giá/m²</p>
            <p className="text-[15px] font-semibold text-gray-900">
              {formatCurrency(Math.round(post.price / post.area))}
            </p>
          </div>
        </div>
      </div>

      {/* Description */}
      <div>
        <h2 className="text-[18px] font-bold text-gray-900 mb-3">Mô tả chi tiết</h2>
        <div className="text-[15px] text-gray-700 leading-relaxed whitespace-pre-line">
          {post.description}
        </div>
      </div>
    </div>
  );
};

export default RoomInformation;

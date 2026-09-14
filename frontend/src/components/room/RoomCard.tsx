import React from 'react';
import { useNavigate } from 'react-router-dom';
import { PostListItem } from '../../types/post.types';
import { formatPrice, getFullAddress } from '../../utils/helpers';
import Card from '../common/Card';

interface RoomCardProps {
  post: PostListItem;
}

const RoomCard: React.FC<RoomCardProps> = ({ post }) => {
  const navigate = useNavigate();

  const handleClick = () => {
    navigate(`/rooms/${post.id}`);
  };

  return (
    <Card hoverable onClick={handleClick}>
      {/* Image */}
      <div className="relative h-48 bg-gray-200">
        {post.thumbnailUrl ? (
          <img
            src={post.thumbnailUrl}
            alt={post.title}
            className="w-full h-full object-cover"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-gray-400">
            <span className="text-6xl">🏠</span>
          </div>
        )}
        
        {/* Price Badge */}
        <div className="absolute top-2 right-2 bg-blue-600 text-white px-3 py-1 rounded-full font-semibold">
          {formatPrice(post.price)}/tháng
        </div>
      </div>

      {/* Content */}
      <div className="p-4">
        <h3 className="text-lg font-semibold text-gray-900 mb-2 line-clamp-2">
          {post.title}
        </h3>

        {/* Address */}
        <p className="text-sm text-gray-600 mb-3 line-clamp-1">
          📍 {post.ward}, {post.district}, {post.province}
        </p>

        {/* Details */}
        <div className="flex items-center justify-between text-sm text-gray-600">
          <span>📐 {post.area}m²</span>
          <span>👥 {post.maxOccupants} người</span>
        </div>
      </div>
    </Card>
  );
};

export default RoomCard;

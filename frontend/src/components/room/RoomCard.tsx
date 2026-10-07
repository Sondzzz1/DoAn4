import React, { useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { FiHeart, FiHome, FiMapPin, FiUsers } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { PostListItem, RoomStatus } from '../../types/post.types';
import RoomAvailabilityBadge from './RoomAvailabilityBadge';
import { formatPrice } from '../../utils/helpers';
import { useAuth } from '../../hooks/useAuth';
import { favoriteService } from '../../services/favoriteService';
import { getApiErrorMessage } from '../../utils/apiError';

interface RoomCardProps {
  post: PostListItem;
  badge?: string;
}

const RoomCard: React.FC<RoomCardProps> = ({ post, badge }) => {
  const navigate = useNavigate();
  const { isAuthenticated, isTenant } = useAuth();
  const [saved, setSaved] = useState(false);
  const [saving, setSaving] = useState(false);
  const savingRef = useRef(false);

  useEffect(() => {
    let active = true;
    if (isTenant) {
      void favoriteService.checkFavorite(post.id).then(response => { if (active) setSaved(!!response.data); }).catch(() => {});
    }
    return () => { active = false; };
  }, [post.id, isTenant]);

  const toggleFavorite = async () => {
    if (!isAuthenticated) { navigate('/login'); return; }
    if (!isTenant) { toast.info('Lưu tin dành cho tài khoản người thuê phòng.'); return; }
    if (savingRef.current) return;
    savingRef.current = true; setSaving(true);
    try {
      if (saved) await favoriteService.removeFavorite(post.id);
      else await favoriteService.addFavorite(post.id);
      setSaved(!saved);
      toast.success(saved ? 'Đã bỏ lưu tin.' : 'Đã lưu phòng vào danh sách yêu thích.');
    } catch (error) { toast.error(getApiErrorMessage(error, 'Không thể cập nhật tin đã lưu.')); }
    finally { savingRef.current = false; setSaving(false); }
  };

  return (
    <article className="room-card">
      {/* Image */}
      <div className="room-card-image">
        <Link to={`/rooms/${post.id}`} aria-label={`Xem ${post.title}`}>
        {post.thumbnailUrl ? (
          <img
            src={post.thumbnailUrl}
            alt={post.title}
            className="w-full h-full object-cover"
            loading="lazy"
            onError={e => { e.currentTarget.onerror = null; e.currentTarget.src = '/room-placeholder.svg'; }}
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-gray-400">
            <FiHome size={36} />
          </div>
        )}
        </Link>
        {/* Price Badge */}
        {badge && post.roomStatus === RoomStatus.Available
          ? <span className="room-card-badge">{badge}</span>
          : <RoomAvailabilityBadge status={post.roomStatus} className="absolute top-3 left-3" />}
        <button type="button" className={`room-card-heart ${saved && isTenant ? 'is-saved' : ''}`} onClick={() => void toggleFavorite()} disabled={saving} aria-pressed={saved && isTenant} aria-label={saved && isTenant ? 'Bỏ lưu tin' : 'Lưu tin'} title={saved && isTenant ? 'Bỏ lưu tin' : 'Lưu tin'}><FiHeart size={18} /></button>
      </div>

      {/* Content */}
      <div className="room-card-content">
        <div className="room-card-meta"><span className="room-card-price">{formatPrice(post.price)}<small>/tháng</small></span><span className="room-card-area">{post.area} m²</span></div>
        <h3 className="room-card-title">
          <Link to={`/rooms/${post.id}`}>{post.title}</Link>
        </h3>

        {/* Address */}
        <p className="room-card-address">
          <FiMapPin size={14} /><span>{[post.district, post.province].filter(Boolean).join(', ') || post.address}</span>
        </p>

        {/* Details */}
        <div className="room-card-foot">
          <span><FiUsers /> {post.maxOccupants} người</span>
          <span>{post.categoryName || 'Phòng trọ'}</span>
        </div>
      </div>
    </article>
  );
};

export default RoomCard;

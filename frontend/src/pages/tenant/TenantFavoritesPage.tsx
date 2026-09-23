import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { FiHeart, FiMapPin, FiTrash2, FiExternalLink, FiSearch, FiHome } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { favoriteService } from '../../services/favoriteService';
import { PostListItem } from '../../types/post.types';
import { formatPrice } from '../../utils/helpers';

const TenantFavoritesPage: React.FC = () => {
  const [favorites, setFavorites] = useState<PostListItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  const loadFavorites = async () => {
    setLoading(true);
    try {
      const response = await favoriteService.getMyFavorites();
      setFavorites(response.data || []);
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể tải danh sách phòng yêu thích.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadFavorites();
  }, []);

  const handleRemoveFavorite = async (postId: number, e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();

    try {
      await favoriteService.removeFavorite(postId);
      toast.info('Đã xóa khỏi danh sách yêu thích');
      setFavorites((prev) => prev.filter((p) => p.id !== postId));
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể xóa phòng yêu thích.');
    }
  };

  return (
    <div className="max-w-6xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <div className="flex items-center gap-2 text-xs uppercase tracking-widest text-rose-500 font-bold mb-1">
            <FiHeart /> Bộ sưu tập của bạn
          </div>
          <h1 className="text-3xl font-extrabold text-slate-900 tracking-tight">Phòng trọ đã lưu</h1>
          <p className="text-sm text-slate-500 mt-1">
            Danh sách các phòng trọ bạn đã đánh dấu yêu thích để dễ dàng theo dõi và so sánh.
          </p>
        </div>

        <div className="bg-rose-50 border border-rose-100 px-4 py-2 rounded-2xl flex items-center gap-2 text-rose-600 font-semibold text-sm">
          <FiHeart className="fill-rose-500" />
          <span>{favorites.length} phòng đã lưu</span>
        </div>
      </div>

      {/* Content */}
      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[1, 2, 3].map((i) => (
            <div key={i} className="bg-white rounded-3xl border border-slate-200 p-4 animate-pulse">
              <div className="h-48 bg-slate-100 rounded-2xl mb-4" />
              <div className="h-6 bg-slate-100 rounded-lg w-3/4 mb-2" />
              <div className="h-4 bg-slate-100 rounded-lg w-1/2" />
            </div>
          ))}
        </div>
      ) : favorites.length === 0 ? (
        <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center shadow-sm max-w-lg mx-auto">
          <div className="w-20 h-20 bg-rose-50 text-rose-500 rounded-full flex items-center justify-center mx-auto mb-4 text-3xl">
            <FiHeart />
          </div>
          <h2 className="text-xl font-bold text-slate-900 mb-2">Chưa có phòng trọ nào được lưu</h2>
          <p className="text-sm text-slate-500 mb-6">
            Hãy khám phá các phòng trọ phù hợp và bấm vào biểu tượng trái tim để lưu lại xem sau.
          </p>
          <Link
            to="/rooms"
            className="inline-flex items-center gap-2 px-6 py-3 bg-[#0084ff] hover:bg-[#0073e6] text-white font-semibold rounded-2xl shadow-md shadow-blue-500/20 transition-all"
          >
            <FiSearch /> Khám phá phòng trọ ngay
          </Link>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {favorites.map((post) => (
            <div
              key={post.id}
              className="bg-white rounded-3xl border border-slate-200 overflow-hidden shadow-sm hover:shadow-xl transition-all group flex flex-col justify-between"
            >
              <div>
                {/* Image */}
                <div className="relative h-52 bg-slate-100 overflow-hidden">
                  {post.thumbnailUrl ? (
                    <img
                      src={post.thumbnailUrl}
                      alt={post.title}
                      className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                    />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center text-slate-400">
                      <FiHome size={48} />
                    </div>
                  )}

                  {/* Price badge */}
                  <div className="absolute top-3 left-3 bg-gradient-to-r from-[#0084ff] to-cyan-500 text-white font-bold px-3.5 py-1.5 rounded-full text-xs shadow-md">
                    {formatPrice(post.price)}/tháng
                  </div>

                  {/* Delete favorite button */}
                  <button
                    type="button"
                    onClick={(e) => handleRemoveFavorite(post.id, e)}
                    title="Bỏ lưu tin"
                    className="absolute top-3 right-3 w-9 h-9 rounded-full bg-white/90 backdrop-blur text-rose-500 hover:bg-rose-500 hover:text-white flex items-center justify-center shadow-md transition-all cursor-pointer border-none"
                  >
                    <FiTrash2 size={16} />
                  </button>
                </div>

                {/* Body */}
                <div className="p-5">
                  <h3 className="font-bold text-slate-900 text-base mb-2 line-clamp-2 hover:text-[#0084ff] transition-colors">
                    <Link to={`/rooms/${post.id}`}>{post.title}</Link>
                  </h3>

                  <div className="flex items-center gap-1.5 text-xs text-slate-500 mb-4 line-clamp-1">
                    <FiMapPin className="text-slate-400 shrink-0" />
                    <span>
                      {post.ward ? `${post.ward}, ` : ''}
                      {post.district ? `${post.district}, ` : ''}
                      {post.province || 'Toàn quốc'}
                    </span>
                  </div>

                  <div className="grid grid-cols-2 gap-2 pt-3 border-t border-slate-100 text-xs text-slate-600 font-medium">
                    <div className="bg-slate-50 rounded-xl p-2 text-center">
                      <span className="text-slate-400 block text-[10px]">Diện tích</span>
                      <span className="font-bold text-slate-800">{post.area} m²</span>
                    </div>
                    <div className="bg-slate-50 rounded-xl p-2 text-center">
                      <span className="text-slate-400 block text-[10px]">Sức chứa</span>
                      <span className="font-bold text-slate-800">{post.maxOccupants} người</span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Action footer */}
              <div className="p-5 pt-0">
                <Link
                  to={`/rooms/${post.id}`}
                  className="w-full flex items-center justify-center gap-2 py-2.5 bg-blue-50 hover:bg-[#0084ff] text-[#0084ff] hover:text-white rounded-xl text-xs font-bold transition-all"
                >
                  <FiExternalLink /> Xem chi tiết phòng
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default TenantFavoritesPage;

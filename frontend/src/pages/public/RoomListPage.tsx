import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { postService } from '../../services/postService';
import { PostListItem } from '../../types/post.types';
import { formatPrice } from '../../utils/helpers';
import Spinner from '../../components/common/Spinner';
import { toast } from 'react-toastify';
import { FiSearch, FiMapPin, FiFilter, FiX } from 'react-icons/fi';

const RoomListPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [posts, setPosts] = useState<PostListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [showFilter, setShowFilter] = useState(false);
  const [filters, setFilters] = useState({
    province: searchParams.get('province') || '',
    district: searchParams.get('district') || '',
    minPrice: '',
    maxPrice: '',
    minArea: '',
    maxArea: '',
  });

  useEffect(() => { fetchPosts(); }, [filters]);

  const fetchPosts = async () => {
    setLoading(true);
    try {
      const params = {
        province: filters.province || undefined,
        district: filters.district || undefined,
        minPrice: filters.minPrice ? parseFloat(filters.minPrice) * 1000000 : undefined,
        maxPrice: filters.maxPrice ? parseFloat(filters.maxPrice) * 1000000 : undefined,
        minArea: filters.minArea ? parseFloat(filters.minArea) : undefined,
        maxArea: filters.maxArea ? parseFloat(filters.maxArea) : undefined,
      };
      const r = await postService.searchPosts(params);
      setPosts(r.data);
    } catch { toast.error('Không thể tải danh sách phòng'); }
    finally { setLoading(false); }
  };

  const reset = () => setFilters({ province:'', district:'', minPrice:'', maxPrice:'', minArea:'', maxArea:'' });

  if (loading) return <Spinner fullScreen/>;

  return (
    <div className="bg-gray-50 min-h-screen min-w-[1200px]">
      {/* Header bar */}
      <div className="bg-white border-b border-gray-200 sticky top-20 z-40 shadow-sm">
        <div className="max-w-[1200px] mx-auto px-4 py-4 flex items-center gap-4">
          <div className="flex-1 flex items-center gap-3 bg-gray-100 rounded-xl px-5 py-3">
            <FiSearch className="w-5 h-5 text-gray-400"/>
            <input type="text" placeholder="Tìm theo khu vực, quận..." className="flex-1 bg-transparent text-[15px] text-gray-700 outline-none placeholder-gray-400"/>
          </div>
          <button onClick={() => setShowFilter(!showFilter)}
            className={'flex items-center gap-2 px-6 py-3 rounded-xl text-[15px] font-bold transition-colors border ' + (showFilter ? 'bg-blue-600 text-white border-blue-600 shadow-md' : 'bg-white text-gray-700 border-gray-200 hover:bg-gray-50')}>
            <FiFilter className="w-5 h-5"/>Bộ lọc
          </button>
        </div>

        {/* Expanded Filter */}
        {showFilter && (
          <div className="border-t border-gray-100 px-4 py-6 bg-gray-50/50">
            <div className="max-w-[1200px] mx-auto flex gap-5">
              {[
                { label: 'Tỉnh/Thành phố', key: 'province', placeholder: 'VD: Hồ Chí Minh' },
                { label: 'Quận/Huyện', key: 'district', placeholder: 'VD: Quận 7' },
                { label: 'Giá thấp nhất (triệu)', key: 'minPrice', placeholder: 'VD: 2' },
                { label: 'Giá cao nhất (triệu)', key: 'maxPrice', placeholder: 'VD: 10' },
              ].map(f => (
                <div key={f.key} className="flex-1">
                  <label className="block text-sm font-bold text-gray-700 mb-2">{f.label}</label>
                  <input type="text" placeholder={f.placeholder} value={(filters as any)[f.key]}
                    onChange={e => setFilters({ ...filters, [f.key]: e.target.value })}
                    className="w-full px-4 py-2.5 border border-gray-200 rounded-xl text-[15px] outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 bg-white"/>
                </div>
              ))}
            </div>
            <div className="max-w-[1200px] mx-auto flex justify-end mt-5">
              <button onClick={reset} className="flex items-center gap-2 px-6 py-2.5 text-[15px] font-bold text-gray-600 hover:text-gray-900 border border-gray-200 rounded-xl bg-white hover:bg-gray-100 transition-colors shadow-sm">
                <FiX className="w-5 h-5"/>Xóa bộ lọc
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Content */}
      <div className="max-w-[1200px] mx-auto px-4 py-10">
        <div className="flex items-center justify-between mb-8">
          <p className="text-[16px] text-gray-600 font-medium">
            Tìm thấy <span className="font-extrabold text-blue-600 text-lg">{posts.length}</span> phòng
          </p>
          <select className="text-[15px] font-medium border border-gray-200 rounded-xl px-4 py-2.5 outline-none bg-white text-gray-800 focus:border-blue-400 shadow-sm cursor-pointer hover:bg-gray-50">
            <option>Mới nhất</option>
            <option>Giá tăng dần</option>
            <option>Giá giảm dần</option>
          </select>
        </div>

        {posts.length === 0 ? (
          <div className="text-center py-24 bg-white rounded-3xl shadow-sm border border-gray-100">
            <div className="text-8xl mb-6">🔍</div>
            <h3 className="text-2xl font-extrabold text-gray-800 mb-3">Không tìm thấy phòng nào</h3>
            <p className="text-gray-500 text-[16px] font-medium mb-6">Thử thay đổi bộ lọc để xem thêm kết quả</p>
            <button onClick={reset} className="px-8 py-3.5 bg-blue-600 text-white rounded-xl text-[16px] font-bold hover:bg-blue-700 transition-colors shadow-md">Xóa bộ lọc</button>
          </div>
        ) : (
          <div className="flex flex-wrap gap-6">
            {posts.map(post => (
              <div key={post.id} onClick={() => navigate('/rooms/' + post.id)}
                className="w-[calc(25%-18px)] bg-white rounded-2xl shadow-sm hover:shadow-xl transition-all duration-300 cursor-pointer group overflow-hidden border border-gray-100 hover:-translate-y-1.5">
                <div className="relative h-52 overflow-hidden">
                  {post.thumbnailUrl
                    ? <img src={post.thumbnailUrl} alt={post.title} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500"/>
                    : <div className="w-full h-full bg-gradient-to-br from-blue-100 to-blue-50 flex items-center justify-center text-7xl">🏠</div>
                  }
                  <div className="absolute top-3 right-3">
                    <span className="bg-blue-600 text-white text-[13px] font-extrabold px-3.5 py-1.5 rounded-full shadow-md">
                      {formatPrice(post.price)}/tháng
                    </span>
                  </div>
                </div>
                <div className="p-5">
                  <h3 className="text-[16px] font-bold text-gray-800 mb-3 line-clamp-2 group-hover:text-blue-600 transition-colors leading-relaxed min-h-[48px]">{post.title}</h3>
                  <p className="text-[14px] text-gray-500 flex items-center gap-1.5 mb-4">
                    <FiMapPin className="w-4 h-4 flex-shrink-0 text-gray-400"/>
                    <span className="line-clamp-1">{post.ward}, {post.district}, {post.province}</span>
                  </p>
                  <div className="flex items-center justify-between text-[13px] text-gray-600 pt-3 border-t border-gray-100 font-bold">
                    <span className="bg-gray-100 px-3 py-1 rounded-md">{post.area} m²</span>
                    <span className="bg-gray-100 px-3 py-1 rounded-md">{post.maxOccupants} người</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
export default RoomListPage;
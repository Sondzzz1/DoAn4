import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { postService } from '../../services/postService';
import { PostListItem } from '../../types/post.types';
import RoomCard from '../../components/room/RoomCard';
import Spinner from '../../components/common/Spinner';
import { toast } from 'react-toastify';

const RoomListPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [posts, setPosts] = useState<PostListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [filters, setFilters] = useState({
    province: searchParams.get('province') || '',
    district: searchParams.get('district') || '',
  });

  useEffect(() => {
    fetchPosts();
  }, [filters]);

  const fetchPosts = async () => {
    setIsLoading(true);
    try {
      const response = await postService.searchPosts(filters);
      setPosts(response.data);
    } catch (error) {
      toast.error('Không thể tải danh sách phòng');
      console.error(error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleFilterChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFilters(prev => ({ ...prev, [name]: value }));
  };

  const handleReset = () => {
    setFilters({ province: '', district: '' });
  };

  if (isLoading) {
    return <Spinner fullScreen />;
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-8">Tìm kiếm phòng trọ</h1>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow-md p-6 mb-8">
        <h2 className="text-lg font-semibold mb-4">Bộ lọc</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Tỉnh/Thành phố
            </label>
            <input
              type="text"
              name="province"
              value={filters.province}
              onChange={handleFilterChange}
              placeholder="VD: Hồ Chí Minh"
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Quận/Huyện
            </label>
            <input
              type="text"
              name="district"
              value={filters.district}
              onChange={handleFilterChange}
              placeholder="VD: Quận 7"
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          <div className="flex items-end">
            <button
              onClick={handleReset}
              className="px-6 py-2 text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors"
            >
              Đặt lại
            </button>
          </div>
        </div>
      </div>

      {/* Results */}
      <div className="mb-4 flex items-center justify-between">
        <p className="text-gray-600">
          Tìm thấy <span className="font-semibold text-blue-600">{posts.length}</span> phòng
        </p>
      </div>

      {/* Room Grid */}
      {posts.length === 0 ? (
        <div className="text-center py-16">
          <div className="text-6xl mb-4">🔍</div>
          <h3 className="text-xl font-semibold text-gray-700 mb-2">
            Không tìm thấy phòng nào
          </h3>
          <p className="text-gray-500">Thử thay đổi bộ lọc để xem thêm kết quả</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {posts.map(post => (
            <RoomCard key={post.id} post={post} />
          ))}
        </div>
      )}
    </div>
  );
};

export default RoomListPage;

import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'react-toastify';
import { 
  FiEdit2, 
  FiEye, 
  FiPlus, 
  FiTrash2, 
  FiFileText,
  FiMapPin,
  FiDollarSign,
  FiHome,
  FiUsers,
  FiClock,
  FiCheckCircle,
  FiXCircle,
  FiRefreshCw
} from 'react-icons/fi';
import { postService } from '../../services/postService';
import { PostListItem, PostStatus, RoomStatus } from '../../types/post.types';

const postStatusLabel = (status: PostStatus) => {
  switch (status) {
    case PostStatus.Pending:
      return { text: 'Chờ duyệt', className: 'bg-yellow-100 text-yellow-700' };
    case PostStatus.Approved:
      return { text: 'Đã duyệt', className: 'bg-green-100 text-green-700' };
    case PostStatus.Rejected:
      return { text: 'Bị từ chối', className: 'bg-red-100 text-red-700' };
    case PostStatus.Hidden:
      return { text: 'Đã ẩn', className: 'bg-gray-100 text-gray-700' };
    case PostStatus.Expired:
      return { text: 'Hết hạn', className: 'bg-slate-100 text-slate-700' };
    default:
      return { text: 'Không rõ', className: 'bg-slate-100 text-slate-700' };
  }
};

const roomStatusLabel = (status: RoomStatus) => {
  switch (status) {
    case RoomStatus.Available:
      return { text: 'Còn trống', className: 'bg-blue-100 text-blue-700' };
    case RoomStatus.Rented:
      return { text: 'Đã cho thuê', className: 'bg-purple-100 text-purple-700' };
    case RoomStatus.TemporarilyUnavailable:
      return { text: 'Tạm ngưng', className: 'bg-orange-100 text-orange-700' };
    default:
      return { text: 'Không rõ', className: 'bg-slate-100 text-slate-700' };
  }
};

const LandlordPostsPage: React.FC = () => {
  const navigate = useNavigate();
  const [posts, setPosts] = useState<PostListItem[]>([]);
  const [loading, setLoading] = useState(true);

  const loadPosts = async () => {
    setLoading(true);
    try {
      const response = await postService.getMyPosts();
      setPosts(response.data || []);
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể tải danh sách tin đăng');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadPosts();
  }, []);

  const handleDelete = async (id: number) => {
    if (!window.confirm('Bạn có chắc muốn xóa tin đăng này?')) return;

    try {
      await postService.deletePost(id);
      toast.success('Đã xóa tin đăng');
      await loadPosts();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể xóa tin đăng');
    }
  };

  const handleView = (id: number) => {
    navigate(`/rooms/${id}`);
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50 to-purple-50 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4 mb-8">
          <div>
            <div className="flex items-center gap-3 mb-2">
              <div className="p-3 bg-gradient-to-br from-blue-500 to-purple-600 rounded-xl shadow-lg">
                <FiFileText className="text-white text-2xl" />
              </div>
              <div>
                <p className="text-xs uppercase tracking-[0.3em] text-blue-600 font-semibold">QUẢN LÝ TIN ĐĂNG</p>
                <h1 className="text-4xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
                  Tin đăng của tôi
                </h1>
              </div>
            </div>
            <p className="text-slate-600 ml-16">Quản lý và theo dõi tất cả tin đăng phòng trọ của bạn</p>
          </div>
          <button
            onClick={() => navigate('/landlord/posts/create')}
            className="inline-flex items-center gap-2 bg-gradient-to-r from-blue-600 to-purple-600 text-white px-6 py-3.5 rounded-xl hover:from-blue-700 hover:to-purple-700 font-semibold shadow-lg hover:shadow-xl transition-all group"
          >
            <FiPlus className="group-hover:rotate-90 transition-transform" /> Đăng tin mới
          </button>
        </div>

        {/* Stats Cards */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
          <div className="bg-white rounded-2xl p-5 border-2 border-blue-100 shadow-md hover:shadow-lg transition-shadow">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-slate-600 mb-1">Tổng tin đăng</p>
                <p className="text-3xl font-bold text-slate-800">{posts.length}</p>
              </div>
              <div className="p-3 bg-blue-100 rounded-xl">
                <FiFileText className="text-blue-600 text-xl" />
              </div>
            </div>
          </div>
          
          <div className="bg-white rounded-2xl p-5 border-2 border-yellow-100 shadow-md hover:shadow-lg transition-shadow">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-slate-600 mb-1">Chờ duyệt</p>
                <p className="text-3xl font-bold text-yellow-600">
                  {posts.filter(p => p.status === PostStatus.Pending).length}
                </p>
              </div>
              <div className="p-3 bg-yellow-100 rounded-xl">
                <FiClock className="text-yellow-600 text-xl" />
              </div>
            </div>
          </div>

          <div className="bg-white rounded-2xl p-5 border-2 border-green-100 shadow-md hover:shadow-lg transition-shadow">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-slate-600 mb-1">Đã duyệt</p>
                <p className="text-3xl font-bold text-green-600">
                  {posts.filter(p => p.status === PostStatus.Approved).length}
                </p>
              </div>
              <div className="p-3 bg-green-100 rounded-xl">
                <FiCheckCircle className="text-green-600 text-xl" />
              </div>
            </div>
          </div>

          <div className="bg-white rounded-2xl p-5 border-2 border-red-100 shadow-md hover:shadow-lg transition-shadow">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-slate-600 mb-1">Bị từ chối</p>
                <p className="text-3xl font-bold text-red-600">
                  {posts.filter(p => p.status === PostStatus.Rejected).length}
                </p>
              </div>
              <div className="p-3 bg-red-100 rounded-xl">
                <FiXCircle className="text-red-600 text-xl" />
              </div>
            </div>
          </div>
        </div>

        <div className="rounded-3xl border-2 border-slate-200 bg-white shadow-xl p-6">
          {loading ? (
            <div className="py-20 text-center">
              <div className="inline-block animate-spin rounded-full h-12 w-12 border-4 border-blue-500 border-t-transparent"></div>
              <p className="text-slate-600 mt-4">Đang tải danh sách tin đăng...</p>
            </div>
          ) : posts.length === 0 ? (
            <div className="py-20 text-center">
              <div className="inline-block p-6 bg-blue-50 rounded-full mb-4">
                <FiFileText className="text-blue-500 text-5xl" />
              </div>
              <p className="text-xl text-slate-700 font-semibold mb-2">Bạn chưa có tin đăng nào</p>
              <p className="text-slate-600 mb-6">Hãy tạo tin đăng đầu tiên để bắt đầu cho thuê phòng trọ</p>
              <button
                onClick={() => navigate('/landlord/posts/create')}
                className="inline-flex items-center gap-2 bg-gradient-to-r from-blue-600 to-purple-600 text-white px-6 py-3 rounded-xl hover:from-blue-700 hover:to-purple-700 font-semibold shadow-lg"
              >
                <FiPlus /> Đăng tin ngay
              </button>
            </div>
          ) : (
            <div className="space-y-5">
              {posts.map((post) => {
                const postStatusInfo = postStatusLabel(post.status);
                const roomStatusInfo = roomStatusLabel(post.roomStatus);

                return (
                  <div
                    key={post.id}
                    className="border-2 border-slate-200 rounded-2xl p-6 hover:shadow-xl hover:border-blue-200 transition-all duration-300 bg-white"
                  >
                    <div className="flex flex-col lg:flex-row gap-6">
                      {/* Thumbnail */}
                      <div className="w-full lg:w-64 h-48 rounded-xl bg-gradient-to-br from-slate-100 to-slate-200 overflow-hidden flex-shrink-0 shadow-md">
                        {post.thumbnailUrl ? (
                          <img
                            src={post.thumbnailUrl}
                            alt={post.title}
                            className="w-full h-full object-cover hover:scale-110 transition-transform duration-300"
                          />
                        ) : (
                          <div className="w-full h-full flex items-center justify-center text-slate-400">
                            <FiEye size={40} />
                          </div>
                        )}
                      </div>

                      {/* Content */}
                      <div className="flex-1">
                        <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3 mb-3">
                          <h3 className="text-2xl font-bold text-slate-900 hover:text-blue-600 transition-colors">
                            {post.title}
                          </h3>
                          <div className="flex flex-wrap gap-2">
                            <span className={`px-4 py-1.5 rounded-full text-xs font-semibold ${postStatusInfo.className} shadow-sm`}>
                              {postStatusInfo.text}
                            </span>
                            <span className={`px-4 py-1.5 rounded-full text-xs font-semibold ${roomStatusInfo.className} shadow-sm`}>
                              {roomStatusInfo.text}
                            </span>
                          </div>
                        </div>

                        <div className="flex items-start gap-2 text-slate-600 mb-4">
                          <FiMapPin className="mt-1 flex-shrink-0 text-blue-500" />
                          <p className="text-sm">
                            {post.address}
                            {post.ward && `, ${post.ward}`}
                            {post.district && `, ${post.district}`}
                            {post.province && `, ${post.province}`}
                          </p>
                        </div>

                        <div className="flex flex-wrap gap-4 mb-4">
                          <div className="flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-green-50 to-emerald-50 rounded-xl border border-green-200">
                            <FiDollarSign className="text-green-600" />
                            <span className="font-bold text-green-700">{post.price.toLocaleString()} đ</span>
                            <span className="text-xs text-green-600">/tháng</span>
                          </div>
                          <div className="flex items-center gap-2 px-4 py-2 bg-blue-50 rounded-xl border border-blue-200">
                            <FiHome className="text-blue-600" />
                            <span className="font-semibold text-blue-700">{post.area} m²</span>
                          </div>
                          <div className="flex items-center gap-2 px-4 py-2 bg-purple-50 rounded-xl border border-purple-200">
                            <FiUsers className="text-purple-600" />
                            <span className="font-semibold text-purple-700">{post.maxOccupants} người</span>
                          </div>
                        </div>

                        <div className="flex items-center gap-4 text-xs text-slate-500 mb-4 pb-4 border-b border-slate-100">
                          <span className="flex items-center gap-1">
                            <FiClock className="text-slate-400" />
                            Đăng: {new Date(post.createdAt).toLocaleDateString('vi-VN')}
                          </span>
                          <span className="flex items-center gap-1">
                            <FiRefreshCw className="text-slate-400" />
                            Cập nhật: {new Date(post.updatedAt).toLocaleDateString('vi-VN')}
                          </span>
                        </div>

                        {/* Actions */}
                        <div className="flex flex-wrap gap-3">
                          <button
                            onClick={() => handleView(post.id)}
                            className="inline-flex items-center gap-2 bg-gradient-to-r from-blue-500 to-blue-600 text-white px-4 py-2.5 rounded-xl text-sm font-semibold hover:from-blue-600 hover:to-blue-700 shadow-md hover:shadow-lg transition-all"
                          >
                            <FiEye /> Xem chi tiết
                          </button>
                          <button
                            onClick={() => navigate(`/landlord/posts/${post.id}/edit`)}
                            className="inline-flex items-center gap-2 bg-gradient-to-r from-amber-500 to-amber-600 text-white px-4 py-2.5 rounded-xl text-sm font-semibold hover:from-amber-600 hover:to-amber-700 shadow-md hover:shadow-lg transition-all"
                          >
                            <FiEdit2 /> Chỉnh sửa
                          </button>
                          <button
                            onClick={() => handleDelete(post.id)}
                            className="inline-flex items-center gap-2 bg-gradient-to-r from-red-500 to-red-600 text-white px-4 py-2.5 rounded-xl text-sm font-semibold hover:from-red-600 hover:to-red-700 shadow-md hover:shadow-lg transition-all"
                          >
                            <FiTrash2 /> Xóa
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
        )}
      </div>
    </div>
    </div>
  );
};

export default LandlordPostsPage;
